namespace GhedDay.Application.Waitlist;

/// <summary>
/// Offers released slots to waitlisted customers and handles SMS acceptance replies.
/// </summary>
public interface IWaitlistOfferService
{
    Task TryOfferReleasedSlotAsync(
        Guid businessId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        int? partySize,
        Guid? offeringId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns true when the inbound SMS was a waitlist acceptance (AI loop should be skipped).
    /// </summary>
    Task<bool> TryAcceptOfferReplyAsync(
        Guid businessId,
        Guid customerId,
        string inboundBody,
        CancellationToken ct = default);

    Task ProcessExpiredOffersAsync(CancellationToken ct = default);
}
