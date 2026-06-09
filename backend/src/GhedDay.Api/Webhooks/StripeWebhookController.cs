using GhedDay.Domain.Entities;
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
    private readonly StripeOptions _stripe;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        GhedDayDbContext db,
        IOptions<StripeOptions> stripe,
        ILogger<StripeWebhookController> logger)
    {
        _db = db;
        _stripe = stripe.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync(ct);

        // 1. Signature verification before any processing.
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

        // 2. Idempotency guard.
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

        // 3. Phase 3: payment_intent.succeeded → confirmed; payment_intent.payment_failed → cancel.
        _logger.LogInformation("Accepted Stripe event {EventId} of type {Type}.", stripeEvent.Id, stripeEvent.Type);

        return Ok();
    }
}
