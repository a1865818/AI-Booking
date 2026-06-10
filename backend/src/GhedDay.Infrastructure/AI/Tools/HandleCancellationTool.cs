using System.Text.Json;
using GhedDay.Application.Bookings;
using GhedDay.Application.Services;
using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Infrastructure.AI.Tools;

/// <summary>Cancels an existing booking for the current customer.</summary>
public sealed class HandleCancellationTool : IClaudeTool
{
    private readonly GhedDayDbContext _db;
    private readonly IBookingRepository _bookings;
    private readonly INotificationService _notifications;

    public HandleCancellationTool(
        GhedDayDbContext db,
        IBookingRepository bookings,
        INotificationService notifications)
    {
        _db = db;
        _bookings = bookings;
        _notifications = notifications;
    }

    public string Name => "handle_cancellation";

    public ToolDefinition GetDefinition(Business business) => new()
    {
        Name = Name,
        Description =
            "Cancel a booking belonging to the current customer. Only works for pending or confirmed future bookings.",
        InputSchema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["booking_id"] = Prop("string", "The booking id to cancel (from a prior tool result)."),
            },
            ["required"] = new[] { "booking_id" },
        },
    };

    public async Task<string> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct = default)
    {
        var bookingId = input.GetGuid("booking_id");
        if (bookingId is null)
            return ToolJson.Serialize(new { success = false, reason = "Invalid or missing 'booking_id'." });

        var booking = await _db.Bookings
            .Where(b => b.Id == bookingId.Value && b.CustomerId == context.CustomerId)
            .Select(b => new { b.Id, b.Status, b.StartTime })
            .FirstOrDefaultAsync(ct);

        if (booking is null)
            return ToolJson.Serialize(new { success = false, reason = "Booking not found for this customer." });

        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed or BookingStatus.NoShow)
            return ToolJson.Serialize(new { success = false, reason = "Booking is already closed." });

        if (booking.StartTime <= DateTimeOffset.UtcNow)
            return ToolJson.Serialize(new { success = false, reason = "Past bookings cannot be cancelled here." });

        var expected = booking.Status;
        var transitioned = await _bookings.TryTransitionStatusAsync(
            context.BusinessId, booking.Id, expected, BookingStatus.Cancelled, ct);

        if (!transitioned)
            return ToolJson.Serialize(new { success = false, reason = "Could not cancel — status may have changed." });

        await _notifications.BookingStatusChangedAsync(
            context.BusinessId, booking.Id, BookingStatus.Cancelled.ToString(), ct);

        return ToolJson.Serialize(new { success = true, booking_id = booking.Id, status = "cancelled" });
    }

    private static Dictionary<string, object?> Prop(string type, string description) =>
        new() { ["type"] = type, ["description"] = description };
}
