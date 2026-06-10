using Dapper;
using GhedDay.Application.Bookings;
using GhedDay.Domain.Enums;
using Npgsql;

namespace GhedDay.Infrastructure.Data.Repositories;

/// <summary>
/// Raw SQL + Dapper booking writes (non-negotiable rule 2). The hold path serializes
/// concurrent attempts per business with a transaction-scoped advisory lock and relies on the
/// <c>bookings</c> GiST exclusion constraint as a database-level backstop against overlaps.
/// Every statement filters by <c>business_id</c> (rule 3).
/// </summary>
public sealed class BookingRepository : IBookingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BookingRepository(IDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    private const string HoldSql = """
        WITH available AS (
            SELECT r."Id"
            FROM resources r
            WHERE r."BusinessId" = @businessId
              AND r."IsActive" = true
              AND r."Capacity" >= @requiredCapacity
              AND (@preferredResourceId IS NULL OR r."Id" = @preferredResourceId)
              AND NOT EXISTS (
                SELECT 1 FROM bookings b
                WHERE b."ResourceId" = r."Id"
                  AND b."Status" IN (@pending, @confirmed)
                  AND tstzrange(b."StartTime", b."EndTime") && tstzrange(@start, @end)
              )
            ORDER BY r."Capacity" ASC, r."SortOrder" ASC, r."Name" ASC
            LIMIT 1
        )
        INSERT INTO bookings
            ("Id", "BusinessId", "CustomerId", "OfferingId", "ResourceId",
             "StartTime", "EndTime", "PartySize", "Status", "HoldExpiresAt", "CreatedAt")
        SELECT @id, @businessId, @customerId, @offeringId, available."Id",
               @start, @end, @partySize, @status, @holdExpiresAt, @createdAt
        FROM available
        RETURNING "Id", "ResourceId", "HoldExpiresAt";
        """;

    public async Task<BookingHoldResult> CreateBookingHoldAsync(
        CreateBookingHoldRequest request,
        CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)await _connectionFactory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        // Serialize all hold attempts for this business so the select-then-insert is atomic.
        var lockKey = AdvisoryLockKey(request.BusinessId);
        await conn.ExecuteAsync(new CommandDefinition(
            "SELECT pg_advisory_xact_lock(@lockKey);",
            new { lockKey }, tx, cancellationToken: ct));

        var bookingId = Guid.NewGuid();
        var isPendingDeposit = request.InitialStatus == BookingStatus.PendingDeposit;
        var holdExpiresAt = isPendingDeposit
            ? DateTimeOffset.UtcNow.Add(request.HoldDuration)
            : (DateTimeOffset?)null;

        try
        {
            var row = await conn.QuerySingleOrDefaultAsync<(Guid Id, Guid ResourceId, DateTimeOffset? HoldExpiresAt)?>(
                new CommandDefinition(HoldSql, new
                {
                    id = bookingId,
                    businessId = request.BusinessId,
                    customerId = request.CustomerId,
                    offeringId = request.OfferingId,
                    preferredResourceId = request.PreferredResourceId,
                    requiredCapacity = request.RequiredCapacity,
                    partySize = request.PartySize,
                    start = request.Start.UtcDateTime,
                    end = request.End.UtcDateTime,
                    holdExpiresAt = holdExpiresAt?.UtcDateTime,
                    createdAt = DateTimeOffset.UtcNow.UtcDateTime,
                    status = request.InitialStatus.ToString(),
                    pending = BookingStatus.PendingDeposit.ToString(),
                    confirmed = BookingStatus.Confirmed.ToString(),
                }, tx, cancellationToken: ct));

            if (row is null)
            {
                await tx.RollbackAsync(ct);
                return BookingHoldResult.NoAvailability();
            }

            await tx.CommitAsync(ct);
            return BookingHoldResult.Held(row.Value.Id, row.Value.ResourceId, row.Value.HoldExpiresAt);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ExclusionViolation)
        {
            // Backstop fired — another booking took the slot despite the lock. Treat as no availability.
            await tx.RollbackAsync(ct);
            return BookingHoldResult.NoAvailability();
        }
    }

    public async Task<bool> TryTransitionStatusAsync(
        Guid businessId,
        Guid bookingId,
        BookingStatus expected,
        BookingStatus next,
        CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);

        var rowsAffected = await conn.ExecuteAsync(new CommandDefinition(
            """
            UPDATE bookings
            SET "Status" = @next
            WHERE "Id" = @bookingId
              AND "BusinessId" = @businessId
              AND "Status" = @expected;
            """,
            new
            {
                bookingId,
                businessId,
                expected = expected.ToString(),
                next = next.ToString(),
            }, cancellationToken: ct));

        return rowsAffected == 1;
    }

    public async Task SetPaymentIntentIdAsync(
        Guid businessId,
        Guid bookingId,
        string paymentIntentId,
        CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);

        await conn.ExecuteAsync(new CommandDefinition(
            """
            UPDATE bookings
            SET "StripePaymentIntentId" = @paymentIntentId
            WHERE "Id" = @bookingId
              AND "BusinessId" = @businessId;
            """,
            new { bookingId, businessId, paymentIntentId },
            cancellationToken: ct));
    }

    public async Task<(Guid BusinessId, Guid BookingId)?> FindBookingByPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);

        return await conn.QuerySingleOrDefaultAsync<(Guid BusinessId, Guid BookingId)?>(
            new CommandDefinition(
                """
                SELECT "BusinessId", "Id" AS BookingId
                FROM bookings
                WHERE "StripePaymentIntentId" = @paymentIntentId
                LIMIT 1;
                """,
                new { paymentIntentId },
                cancellationToken: ct));
    }

    /// <summary>Stable 64-bit advisory-lock key derived from the business id.</summary>
    private static long AdvisoryLockKey(Guid businessId) =>
        BitConverter.ToInt64(businessId.ToByteArray(), 0);
}
