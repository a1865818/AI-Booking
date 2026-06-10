namespace GhedDay.Application.Bookings;

using GhedDay.Domain.Enums;

/// <summary>
/// Inputs for a concurrency-safe booking hold. Tenant identifiers are bound server-side from
/// <c>ITenantContext</c> before this is constructed — never from Claude tool args
/// (non-negotiable rule 1).
/// </summary>
public sealed record CreateBookingHoldRequest
{
    public required Guid BusinessId { get; init; }
    public required Guid CustomerId { get; init; }

    /// <summary>Null for resource-only bookings (e.g. a plain table reservation).</summary>
    public Guid? OfferingId { get; init; }

    /// <summary>When set, only this resource is considered; otherwise the smallest sufficient free resource is chosen.</summary>
    public Guid? PreferredResourceId { get; init; }

    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }

    /// <summary>1 for service bookings; party_size for restaurants.</summary>
    public int RequiredCapacity { get; init; } = 1;

    public int? PartySize { get; init; }

    public required TimeSpan HoldDuration { get; init; }

    /// <summary>
    /// <see cref="BookingStatus.PendingDeposit"/> for deposit holds; <see cref="BookingStatus.Confirmed"/>
    /// when no deposit is required (restaurant small parties). Confirmed bookings skip hold expiry.
    /// </summary>
    public BookingStatus InitialStatus { get; init; } = BookingStatus.PendingDeposit;
}

/// <summary>Outcome of a hold attempt. <see cref="Success"/> is false when no resource was free.</summary>
public sealed record BookingHoldResult
{
    public bool Success { get; init; }
    public Guid? BookingId { get; init; }
    public Guid? ResourceId { get; init; }
    public DateTimeOffset? HoldExpiresAt { get; init; }
    public string? FailureReason { get; init; }

    public static BookingHoldResult Held(Guid bookingId, Guid resourceId, DateTimeOffset? holdExpiresAt) =>
        new()
        {
            Success = true,
            BookingId = bookingId,
            ResourceId = resourceId,
            HoldExpiresAt = holdExpiresAt,
        };

    public static BookingHoldResult NoAvailability(string reason = "No resource is available for the requested time.") =>
        new() { Success = false, FailureReason = reason };
}
