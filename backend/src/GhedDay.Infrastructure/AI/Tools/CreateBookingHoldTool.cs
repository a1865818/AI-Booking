using System.Globalization;
using System.Text.Json;
using GhedDay.Application.Bookings;
using GhedDay.Application.DTOs;
using GhedDay.Application.Services;
using GhedDay.Application.Verticals;
using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Infrastructure.AI.Tools;

/// <summary>
/// Reserves a slot as a <c>pending_deposit</c> hold. The slot is re-validated server-side under
/// an advisory lock by <see cref="IBookingRepository"/> regardless of what Claude passed in —
/// the last line of defence against hallucinated availability (PLAN §2.5 risk).
/// </summary>
public sealed class CreateBookingHoldTool : IClaudeTool
{
    private readonly GhedDayDbContext _db;
    private readonly IBookingRepository _bookings;
    private readonly IVerticalConfigService _verticalConfig;
    private readonly INotificationService _notifications;

    public CreateBookingHoldTool(
        GhedDayDbContext db,
        IBookingRepository bookings,
        IVerticalConfigService verticalConfig,
        INotificationService notifications)
    {
        _db = db;
        _bookings = bookings;
        _verticalConfig = verticalConfig;
        _notifications = notifications;
    }

    public string Name => "create_booking_hold";

    public ToolDefinition GetDefinition(Business business) => new()
    {
        Name = Name,
        Description =
            "Place a short hold on a specific available slot. Only call with a slot returned by check_availability. " +
            "offering_id is optional for resource-only reservations.",
        InputSchema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["slot_iso"] = Prop("string", "The chosen start time as an ISO-8601 string from check_availability."),
                ["offering_id"] = Prop("string", "Optional offering id."),
                ["party_size"] = Prop("integer", "Number of guests (restaurants); defaults to 1."),
            },
            ["required"] = new[] { "slot_iso" },
        },
    };

    public async Task<string> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct = default)
    {
        var business = context.Business;
        var config = _verticalConfig.GetConfig(business);

        var slotRaw = input.GetString("slot_iso");
        if (!DateTimeOffset.TryParse(slotRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var start))
            return ToolJson.Serialize(new { success = false, reason = "Invalid or missing 'slot_iso'." });

        var partySize = input.GetInt("party_size");
        var requiredCapacity = partySize is > 0 ? partySize.Value : 1;

        var offeringId = input.GetGuid("offering_id");
        var durationMinutes = await ResolveDurationAsync(offeringId, business.Id, config, ct);
        var end = start.AddMinutes(durationMinutes);

        var result = await _bookings.CreateBookingHoldAsync(new CreateBookingHoldRequest
        {
            BusinessId = context.BusinessId,
            CustomerId = context.CustomerId,
            OfferingId = offeringId,
            Start = start,
            End = end,
            RequiredCapacity = requiredCapacity,
            PartySize = partySize,
            HoldDuration = TimeSpan.FromMinutes(config.HoldMinutes),
        }, ct);

        if (!result.Success)
            return ToolJson.Serialize(new { success = false, reason = result.FailureReason });

        var depositRequired = _verticalConfig.RequiresDeposit(business, partySize);
        var depositCents = _verticalConfig.GetDepositCents(business, partySize);

        await _notifications.BookingCreatedAsync(context.BusinessId, new BookingDto(
            result.BookingId!.Value, context.CustomerId, offeringId, result.ResourceId,
            start, end, partySize, BookingStatus.PendingDeposit, result.HoldExpiresAt), ct);

        return ToolJson.Serialize(new
        {
            success = true,
            booking_id = result.BookingId,
            resource_id = result.ResourceId,
            start = start.ToString("o"),
            hold_expires_at = result.HoldExpiresAt?.ToString("o"),
            status = "pending_deposit",
            deposit_required = depositRequired,
            deposit_cents = depositCents,
        });
    }

    private async Task<int> ResolveDurationAsync(Guid? offeringId, Guid businessId, Domain.ValueObjects.VerticalConfig config, CancellationToken ct)
    {
        if (offeringId is { } id)
        {
            var duration = await _db.Offerings
                .Where(o => o.Id == id)
                .Select(o => (int?)o.DurationMinutes)
                .FirstOrDefaultAsync(ct);
            if (duration is > 0)
                return duration.Value;
        }

        return config.DefaultDurationMinutes ?? 60;
    }

    private static Dictionary<string, object?> Prop(string type, string description) =>
        new() { ["type"] = type, ["description"] = description };
}
