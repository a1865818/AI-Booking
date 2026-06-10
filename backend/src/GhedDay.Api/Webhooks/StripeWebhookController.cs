using GhedDay.Application.Bookings;
using GhedDay.Application.Services;
using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Infrastructure.Configuration;
using GhedDay.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace GhedDay.Api.Webhooks;

/// <summary>
/// Stripe webhook. Order is mandatory (non-negotiable rules 2, 4):
///   1. Verify the signature with EventUtility.ConstructEvent FIRST.
///   2. Insert into processed_events FIRST; a unique violation → 200 no-op.
///   3. payment_intent.succeeded → guarded transition to confirmed (Phase 3).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("webhooks/stripe")]
public sealed class StripeWebhookController : ControllerBase
{
    private readonly GhedDayDbContext _db;
    private readonly IBookingRepository _bookings;
    private readonly INotificationService _notifications;
    private readonly StripeOptions _stripe;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        GhedDayDbContext db,
        IBookingRepository bookings,
        INotificationService notifications,
        IOptions<StripeOptions> stripe,
        ILogger<StripeWebhookController> logger)
    {
        _db = db;
        _bookings = bookings;
        _notifications = notifications;
        _stripe = stripe.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync(ct);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripe.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Rejected Stripe webhook with invalid signature.");
            return BadRequest();
        }

        _db.ProcessedEvents.Add(new ProcessedEvent { Id = stripeEvent.Id, Source = "stripe" });
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _logger.LogInformation("Duplicate Stripe event {EventId}; returning 200 no-op.", stripeEvent.Id);
            return Ok();
        }

        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                await HandlePaymentSucceededAsync(stripeEvent, ct);
                break;
            case "payment_intent.payment_failed":
                await HandlePaymentFailedAsync(stripeEvent, ct);
                break;
            default:
                _logger.LogInformation(
                    "Accepted Stripe event {EventId} of type {Type}.",
                    stripeEvent.Id,
                    stripeEvent.Type);
                break;
        }

        return Ok();
    }

    private async Task HandlePaymentSucceededAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not PaymentIntent intent)
            return;

        var bookingRef = await ResolveBookingRefAsync(intent.Metadata, intent.Id, ct);
        if (bookingRef is null)
        {
            _logger.LogWarning("payment_intent.succeeded {IntentId} has no booking metadata.", intent.Id);
            return;
        }

        var (businessId, bookingId) = bookingRef.Value;
        var transitioned = await _bookings.TryTransitionStatusAsync(
            businessId, bookingId, BookingStatus.PendingDeposit, BookingStatus.Confirmed, ct);

        if (transitioned)
        {
            await _notifications.BookingStatusChangedAsync(
                businessId, bookingId, BookingStatus.Confirmed.ToString(), ct);
            _logger.LogInformation("Booking {BookingId} confirmed via Stripe payment {IntentId}.", bookingId, intent.Id);
        }
    }

    private async Task HandlePaymentFailedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not PaymentIntent intent)
            return;

        var bookingRef = await ResolveBookingRefAsync(intent.Metadata, intent.Id, ct);
        if (bookingRef is null)
            return;

        var (businessId, bookingId) = bookingRef.Value;
        var transitioned = await _bookings.TryTransitionStatusAsync(
            businessId, bookingId, BookingStatus.PendingDeposit, BookingStatus.Cancelled, ct);

        if (transitioned)
        {
            await _notifications.BookingStatusChangedAsync(
                businessId, bookingId, BookingStatus.Cancelled.ToString(), ct);
            _logger.LogInformation("Booking {BookingId} cancelled after failed Stripe payment {IntentId}.", bookingId, intent.Id);
        }
    }

    private async Task<(Guid BusinessId, Guid BookingId)?> ResolveBookingRefAsync(
        IDictionary<string, string> metadata,
        string paymentIntentId,
        CancellationToken ct)
    {
        if (metadata.TryGetValue("booking_id", out var bookingRaw)
            && metadata.TryGetValue("business_id", out var businessRaw)
            && Guid.TryParse(bookingRaw, out var bookingId)
            && Guid.TryParse(businessRaw, out var businessId))
        {
            return (businessId, bookingId);
        }

        return await _bookings.FindBookingByPaymentIntentAsync(paymentIntentId, ct);
    }
}
