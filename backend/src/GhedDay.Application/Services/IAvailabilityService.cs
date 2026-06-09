using GhedDay.Application.DTOs;

namespace GhedDay.Application.Services;

/// <summary>
/// Resource-capacity-aware availability, covering all verticals with a single query
/// (party_size = 1 for service bookings; N for restaurant tables).
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Returns resources free for the requested window. <paramref name="requiredCapacity"/>
    /// is 1 for service bookings and party_size for restaurants.
    /// </summary>
    Task<IReadOnlyList<SlotDto>> GetAvailableSlotsAsync(
        Guid businessId,
        DateTimeOffset start,
        DateTimeOffset end,
        int requiredCapacity,
        CancellationToken ct = default);
}
