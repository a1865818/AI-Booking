namespace GhedDay.Application.Waitlist;

public interface IWaitlistRepository
{
    Task<Guid> AddEntryAsync(AddWaitlistEntryRequest request, CancellationToken ct = default);

    Task<WaitlistEntryRow?> GetNextWaitingAsync(
        Guid businessId,
        DateOnly preferredDate,
        int requiredCapacity,
        Guid? offeringId,
        CancellationToken ct = default);

    Task<bool> OfferSlotAsync(
        Guid businessId,
        Guid entryId,
        DateTimeOffset slotStart,
        DateTimeOffset offerExpiresAt,
        CancellationToken ct = default);

    Task<WaitlistEntryRow?> GetActiveOfferForCustomerAsync(
        Guid businessId,
        Guid customerId,
        CancellationToken ct = default);

    Task<bool> MarkBookedAsync(Guid businessId, Guid entryId, CancellationToken ct = default);

    Task<bool> MarkExpiredAsync(Guid businessId, Guid entryId, CancellationToken ct = default);

    Task<IReadOnlyList<ExpiredOfferRow>> ExpireTimedOutOffersAsync(CancellationToken ct = default);
}

public sealed record AddWaitlistEntryRequest
{
    public required Guid BusinessId { get; init; }
    public required Guid CustomerId { get; init; }
    public Guid? OfferingId { get; init; }
    public required DateOnly PreferredDate { get; init; }
    public int? PartySize { get; init; }
}

public sealed record WaitlistEntryRow(
    Guid Id,
    Guid BusinessId,
    Guid CustomerId,
    Guid? OfferingId,
    DateOnly PreferredDate,
    int? PartySize,
    DateTimeOffset? OfferedSlotTime,
    DateTimeOffset? OfferExpiresAt,
    string Status);

public sealed record ExpiredOfferRow(
    Guid Id,
    Guid BusinessId,
    DateOnly PreferredDate,
    int? PartySize,
    Guid? OfferingId,
    DateTimeOffset? OfferedSlotTime);
