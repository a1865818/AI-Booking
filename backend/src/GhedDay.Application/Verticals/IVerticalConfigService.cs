using GhedDay.Domain.Entities;
using GhedDay.Domain.ValueObjects;

namespace GhedDay.Application.Verticals;

/// <summary>
/// Encapsulates all vertical-specific behaviour behind one interface. New verticals are
/// onboarded by extending <c>vertical_config</c> and this service — never by branching on
/// <c>BusinessType</c> in business logic (non-negotiable rule 5).
/// </summary>
public interface IVerticalConfigService
{
    VerticalConfig GetConfig(Business business);

    /// <summary>Plural resource label for UI / Claude copy, e.g. "Tables", "Chairs".</summary>
    string GetResourceLabel(Business business, bool plural = false);

    /// <summary>
    /// Whether a deposit is required for a booking. <paramref name="partySize"/> matters for
    /// restaurants, where deposits kick in at or above a threshold.
    /// </summary>
    bool RequiresDeposit(Business business, int? partySize = null);

    /// <summary>Deposit amount in cents for a booking, or 0 when none is required.</summary>
    int GetDepositCents(Business business, int? partySize = null);

    AvailabilityParams GetAvailabilityParams(Business business, int? partySize = null);

    /// <summary>A short hint injected into the Claude system prompt for this vertical.</summary>
    string GetClaudePersonaHint(Business business);
}

/// <summary>Parameters that shape an availability query for a given vertical/request.</summary>
public sealed record AvailabilityParams(int RequiredCapacity, int DefaultDurationMinutes);
