using System.Globalization;
using System.Text.Json;
using GhedDay.Application.Verticals;
using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Infrastructure.AI.Tools;

/// <summary>
/// Returns open start times for a date, capacity-aware. Reads hours/resources/bookings via EF
/// (tenant-scoped) and computes free windows in the business's timezone. <c>party_size</c> is
/// used for restaurants.
/// </summary>
public sealed class CheckAvailabilityTool : IClaudeTool
{
    private const int IntervalMinutes = 30;
    private const int MaxSlots = 8;

    private readonly GhedDayDbContext _db;
    private readonly IVerticalConfigService _verticalConfig;
    private readonly TimeProvider _clock;

    public CheckAvailabilityTool(GhedDayDbContext db, IVerticalConfigService verticalConfig, TimeProvider clock)
    {
        _db = db;
        _verticalConfig = verticalConfig;
        _clock = clock;
    }

    public string Name => "check_availability";

    public ToolDefinition GetDefinition(Business business)
    {
        var label = _verticalConfig.GetResourceLabel(business).ToLowerInvariant();
        return new ToolDefinition
        {
            Name = Name,
            Description =
                $"Find open start times for a given date before offering them. Returns ISO-8601 start times for which a {label} is free. " +
                "Always call this before proposing times; never invent availability.",
            InputSchema = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>
                {
                    ["offering_id"] = Prop("string", "Optional offering id; determines the booking duration."),
                    ["date"] = Prop("string", "The date to check, in YYYY-MM-DD (business local time)."),
                    ["party_size"] = Prop("integer", "Number of guests (restaurants); defaults to 1."),
                },
                ["required"] = new[] { "date" },
            },
        };
    }

    public async Task<string> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct = default)
    {
        var business = context.Business;
        var config = _verticalConfig.GetConfig(business);

        var dateRaw = input.GetString("date");
        if (!DateOnly.TryParse(dateRaw, CultureInfo.InvariantCulture, out var date))
            return ToolJson.Serialize(new { error = "Invalid or missing 'date'; expected YYYY-MM-DD." });

        var partySize = input.GetInt("party_size");
        var requiredCapacity = partySize is > 0 ? partySize.Value : 1;

        var offeringId = input.GetGuid("offering_id");
        var durationMinutes = await ResolveDurationAsync(offeringId, config, ct);

        var tz = ResolveTimeZone(business.Timezone);
        var weekday = (int)date.DayOfWeek; // Sunday = 0, matches the schema

        var hours = await _db.BusinessHours
            .Where(h => h.DayOfWeek == weekday)
            .Select(h => new { h.OpenTime, h.CloseTime })
            .ToListAsync(ct);

        if (hours.Count == 0)
            return ToolJson.Serialize(new { resource_label = config.ResourceLabel, date = dateRaw, slots = Array.Empty<string>(), message = "Closed on this date." });

        var resources = await _db.Resources
            .Where(r => r.IsActive && r.Capacity >= requiredCapacity)
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (resources.Count == 0)
            return ToolJson.Serialize(new { resource_label = config.ResourceLabel, date = dateRaw, slots = Array.Empty<string>(), message = "No resource large enough." });

        var dayStartUtc = ToUtc(date, new TimeOnly(0, 0), tz);
        var dayEndUtc = ToUtc(date.AddDays(1), new TimeOnly(0, 0), tz);

        var bookings = await _db.Bookings
            .Where(b => (b.Status == BookingStatus.PendingDeposit || b.Status == BookingStatus.Confirmed)
                        && b.ResourceId != null
                        && b.StartTime < dayEndUtc && b.EndTime > dayStartUtc)
            .Select(b => new { ResourceId = b.ResourceId!.Value, b.StartTime, b.EndTime })
            .ToListAsync(ct);

        var now = _clock.GetUtcNow();
        var slots = new List<string>();

        foreach (var window in hours.OrderBy(h => h.OpenTime))
        {
            var candidate = window.OpenTime;
            while (candidate.AddMinutes(durationMinutes) <= window.CloseTime)
            {
                var startUtc = ToUtc(date, candidate, tz);
                var endUtc = startUtc.AddMinutes(durationMinutes);

                if (startUtc >= now)
                {
                    var free = resources.Any(rid =>
                        !bookings.Any(b => b.ResourceId == rid && b.StartTime < endUtc && startUtc < b.EndTime));

                    if (free)
                    {
                        slots.Add(startUtc.ToOffset(tz.GetUtcOffset(startUtc)).ToString("yyyy-MM-ddTHH:mm:sszzz"));
                        if (slots.Count >= MaxSlots)
                            break;
                    }
                }

                candidate = candidate.AddMinutes(IntervalMinutes);
            }

            if (slots.Count >= MaxSlots)
                break;
        }

        return ToolJson.Serialize(new
        {
            resource_label = config.ResourceLabel,
            date = dateRaw,
            duration_minutes = durationMinutes,
            slots,
        });
    }

    private async Task<int> ResolveDurationAsync(Guid? offeringId, Domain.ValueObjects.VerticalConfig config, CancellationToken ct)
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

    internal static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.Utc; }
    }

    internal static DateTimeOffset ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo tz)
    {
        var unspecified = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, tz), TimeSpan.Zero);
    }
}
