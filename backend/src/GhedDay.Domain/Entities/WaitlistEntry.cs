using GhedDay.Domain.Enums;

namespace GhedDay.Domain.Entities;

public class WaitlistEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? OfferingId { get; set; }
    public DateOnly PreferredDate { get; set; }
    public int? PartySize { get; set; }
    public DateTimeOffset? OfferedSlotTime { get; set; }
    public DateTimeOffset? OfferExpiresAt { get; set; }
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Customer? Customer { get; set; }
    public Offering? Offering { get; set; }
}
