using GhedDay.Domain.Enums;

namespace GhedDay.Application.Bookings;

/// <summary>
/// Booking write paths that need raw SQL + advisory locks + guarded transitions, bypassing EF
/// change-tracking by design (non-negotiable rule 2).
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Atomically selects a free resource and inserts a <c>pending_deposit</c> hold under a
    /// per-business advisory lock, with the database overlap-exclusion constraint as a backstop.
    /// Returns <see cref="BookingHoldResult.NoAvailability"/> when nothing is free.
    /// </summary>
    Task<BookingHoldResult> CreateBookingHoldAsync(CreateBookingHoldRequest request, CancellationToken ct = default);

    /// <summary>
    /// Guarded status transition: <c>UPDATE ... WHERE id = @id AND business_id = @businessId
    /// AND status = @expected</c>. Returns true only when exactly one row changed.
    /// </summary>
    Task<bool> TryTransitionStatusAsync(
        Guid businessId,
        Guid bookingId,
        BookingStatus expected,
        BookingStatus next,
        CancellationToken ct = default);
}
