using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Domain.ValueObjects;

namespace GhedDay.Application.Verticals;

/// <summary>
/// Reads <c>Business.vertical_config</c> (+ <c>business_type</c> for default persona hints)
/// to answer vertical-specific questions. Contains no per-vertical branching for booking
/// logic — only sensible default copy when config is absent.
/// </summary>
public sealed class VerticalConfigService : IVerticalConfigService
{
    private const int DefaultDurationMinutes = 60;

    public VerticalConfig GetConfig(Business business) => business.GetVerticalConfig();

    public string GetResourceLabel(Business business, bool plural = false)
    {
        var config = GetConfig(business);
        return plural ? config.ResourceLabelPlural : config.ResourceLabel;
    }

    public bool RequiresDeposit(Business business, int? partySize = null)
    {
        var config = GetConfig(business);
        if (config.DepositRequired)
            return true;

        // Restaurant-style threshold: deposit only for large parties.
        if (config.DepositThresholdPartySize is { } threshold && partySize is { } size)
            return size >= threshold;

        return false;
    }

    public int GetDepositCents(Business business, int? partySize = null)
    {
        if (!RequiresDeposit(business, partySize))
            return 0;

        var config = GetConfig(business);

        // Per-head deposit (restaurants).
        if (config.DepositPerHeadCents is { } perHead && partySize is { } size)
            return perHead * size;

        return config.DepositCents ?? 0;
    }

    public AvailabilityParams GetAvailabilityParams(Business business, int? partySize = null)
    {
        var config = GetConfig(business);
        var requiredCapacity = partySize is { } size && size > 0 ? size : 1;
        var duration = config.DefaultDurationMinutes ?? DefaultDurationMinutes;
        return new AvailabilityParams(requiredCapacity, duration);
    }

    public string GetClaudePersonaHint(Business business)
    {
        var config = GetConfig(business);
        var resource = config.ResourceLabel;

        return business.BusinessType switch
        {
            BusinessType.Restaurant =>
                $"This is a restaurant. Ask for party size before checking availability. " +
                $"Bookings reserve a {resource.ToLowerInvariant()}.",
            BusinessType.NailSalon =>
                "This is a nail salon. Ask which service the customer wants, then offer available times.",
            BusinessType.Barbershop =>
                "This is a barbershop. Ask which service the customer wants, then offer available times.",
            BusinessType.Spa =>
                "This is a spa. Ask which treatment the customer wants, then offer available times.",
            BusinessType.Beauty =>
                "This is a beauty studio. Ask which service the customer wants, then offer available times.",
            _ =>
                $"Help the customer book a {resource.ToLowerInvariant()} at an available time."
        };
    }
}
