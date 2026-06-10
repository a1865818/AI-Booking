using Dapper;
using GhedDay.Application.Waitlist;
using GhedDay.Domain.Enums;

namespace GhedDay.Infrastructure.Data.Repositories;

public sealed class WaitlistRepository : IWaitlistRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public WaitlistRepository(IDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task<Guid> AddEntryAsync(AddWaitlistEntryRequest request, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        await using var conn = await _connectionFactory.OpenAsync(ct);

        await conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO waitlist_entries
                ("Id", "BusinessId", "CustomerId", "OfferingId", "PreferredDate",
                 "PartySize", "Status", "CreatedAt")
            VALUES
                (@id, @businessId, @customerId, @offeringId, @preferredDate,
                 @partySize, @waiting, @createdAt);
            """,
            new
            {
                id,
                businessId = request.BusinessId,
                customerId = request.CustomerId,
                offeringId = request.OfferingId,
                preferredDate = request.PreferredDate,
                partySize = request.PartySize,
                waiting = WaitlistStatus.Waiting.ToString(),
                createdAt = DateTimeOffset.UtcNow.UtcDateTime,
            },
            cancellationToken: ct));

        return id;
    }

    public async Task<WaitlistEntryRow?> GetNextWaitingAsync(
        Guid businessId,
        DateOnly preferredDate,
        int requiredCapacity,
        Guid? offeringId,
        CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);

        return await conn.QuerySingleOrDefaultAsync<WaitlistEntryRow>(new CommandDefinition(
            """
            SELECT w."Id" AS Id,
                   w."BusinessId" AS BusinessId,
                   w."CustomerId" AS CustomerId,
                   w."OfferingId" AS OfferingId,
                   w."PreferredDate" AS PreferredDate,
                   w."PartySize" AS PartySize,
                   w."OfferedSlotTime" AS OfferedSlotTime,
                   w."OfferExpiresAt" AS OfferExpiresAt,
                   w."Status" AS Status
            FROM waitlist_entries w
            WHERE w."BusinessId" = @businessId
              AND w."PreferredDate" = @preferredDate
              AND w."Status" = @waiting
              AND COALESCE(w."PartySize", 1) <= @requiredCapacity
              AND (@offeringId IS NULL OR w."OfferingId" IS NULL OR w."OfferingId" = @offeringId)
            ORDER BY w."CreatedAt" ASC
            LIMIT 1;
            """,
            new
            {
                businessId,
                preferredDate,
                requiredCapacity,
                offeringId,
                waiting = WaitlistStatus.Waiting.ToString(),
            },
            cancellationToken: ct));
    }

    public async Task<bool> OfferSlotAsync(
        Guid businessId,
        Guid entryId,
        DateTimeOffset slotStart,
        DateTimeOffset offerExpiresAt,
        CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);

        var rows = await conn.ExecuteAsync(new CommandDefinition(
            """
            UPDATE waitlist_entries
            SET "Status" = @offered,
                "OfferedSlotTime" = @slotStart,
                "OfferExpiresAt" = @offerExpiresAt
            WHERE "Id" = @entryId
              AND "BusinessId" = @businessId
              AND "Status" = @waiting;
            """,
            new
            {
                entryId,
                businessId,
                slotStart = slotStart.UtcDateTime,
                offerExpiresAt = offerExpiresAt.UtcDateTime,
                waiting = WaitlistStatus.Waiting.ToString(),
                offered = WaitlistStatus.Offered.ToString(),
            },
            cancellationToken: ct));

        return rows == 1;
    }

    public async Task<WaitlistEntryRow?> GetActiveOfferForCustomerAsync(
        Guid businessId,
        Guid customerId,
        CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);

        return await conn.QuerySingleOrDefaultAsync<WaitlistEntryRow>(new CommandDefinition(
            """
            SELECT w."Id" AS Id,
                   w."BusinessId" AS BusinessId,
                   w."CustomerId" AS CustomerId,
                   w."OfferingId" AS OfferingId,
                   w."PreferredDate" AS PreferredDate,
                   w."PartySize" AS PartySize,
                   w."OfferedSlotTime" AS OfferedSlotTime,
                   w."OfferExpiresAt" AS OfferExpiresAt,
                   w."Status" AS Status
            FROM waitlist_entries w
            WHERE w."BusinessId" = @businessId
              AND w."CustomerId" = @customerId
              AND w."Status" = @offered
              AND w."OfferExpiresAt" > @now
            ORDER BY w."OfferExpiresAt" ASC
            LIMIT 1;
            """,
            new
            {
                businessId,
                customerId,
                offered = WaitlistStatus.Offered.ToString(),
                now = DateTimeOffset.UtcNow.UtcDateTime,
            },
            cancellationToken: ct));
    }

    public async Task<bool> MarkBookedAsync(Guid businessId, Guid entryId, CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);

        var rows = await conn.ExecuteAsync(new CommandDefinition(
            """
            UPDATE waitlist_entries
            SET "Status" = @booked
            WHERE "Id" = @entryId
              AND "BusinessId" = @businessId
              AND "Status" = @offered;
            """,
            new
            {
                entryId,
                businessId,
                offered = WaitlistStatus.Offered.ToString(),
                booked = WaitlistStatus.Booked.ToString(),
            },
            cancellationToken: ct));

        return rows == 1;
    }

    public async Task<bool> MarkExpiredAsync(Guid businessId, Guid entryId, CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);

        var rows = await conn.ExecuteAsync(new CommandDefinition(
            """
            UPDATE waitlist_entries
            SET "Status" = @expired
            WHERE "Id" = @entryId
              AND "BusinessId" = @businessId
              AND "Status" = @offered;
            """,
            new
            {
                entryId,
                businessId,
                offered = WaitlistStatus.Offered.ToString(),
                expired = WaitlistStatus.Expired.ToString(),
            },
            cancellationToken: ct));

        return rows == 1;
    }

    public async Task<IReadOnlyList<ExpiredOfferRow>> ExpireTimedOutOffersAsync(CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);

        var rows = await conn.QueryAsync<ExpiredOfferRow>(new CommandDefinition(
            """
            UPDATE waitlist_entries
            SET "Status" = @expired
            WHERE "Status" = @offered
              AND "OfferExpiresAt" <= @now
            RETURNING "Id" AS Id,
                      "BusinessId" AS BusinessId,
                      "PreferredDate" AS PreferredDate,
                      "PartySize" AS PartySize,
                      "OfferingId" AS OfferingId,
                      "OfferedSlotTime" AS OfferedSlotTime;
            """,
            new
            {
                offered = WaitlistStatus.Offered.ToString(),
                expired = WaitlistStatus.Expired.ToString(),
                now = DateTimeOffset.UtcNow.UtcDateTime,
            },
            cancellationToken: ct));

        return rows.ToList();
    }
}
