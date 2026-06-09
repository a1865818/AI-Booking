using GhedDay.Domain.Enums;

namespace GhedDay.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }

    /// <summary>Null for resource-only bookings (e.g. a plain table reservation).</summary>
    public Guid? OfferingId { get; set; }

    public Guid? ResourceId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    /// <summary>Restaurants only.</summary>
    public int? PartySize { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.PendingDeposit;
    public string? StripePaymentIntentId { get; set; }
    public DateTimeOffset? HoldExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Customer? Customer { get; set; }
    public Offering? Offering { get; set; }
    public Resource? Resource { get; set; }
}
