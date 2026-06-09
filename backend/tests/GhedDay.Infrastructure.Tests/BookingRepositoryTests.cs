using Dapper;
using GhedDay.Application.Bookings;
using GhedDay.Domain.Enums;
using GhedDay.Infrastructure.Data.Repositories;
using Npgsql;

namespace GhedDay.Infrastructure.Tests;

[Collection("postgres")]
public sealed class BookingRepositoryTests
{
    private readonly PostgresFixture _pg;
    private readonly BookingRepository _sut;

    private static readonly DateTimeOffset SlotStart =
        new(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SlotEnd =
        new(2030, 1, 1, 11, 0, 0, TimeSpan.Zero);

    public BookingRepositoryTests(PostgresFixture pg)
    {
        _pg = pg;
        _sut = new BookingRepository(_pg.ConnectionFactory);
    }

    private CreateBookingHoldRequest Request(SeededBusiness biz, int capacity, Guid? preferred = null) => new()
    {
        BusinessId = biz.BusinessId,
        CustomerId = biz.CustomerId,
        OfferingId = null,
        PreferredResourceId = preferred,
        Start = SlotStart,
        End = SlotEnd,
        RequiredCapacity = capacity,
        PartySize = capacity,
        HoldDuration = TimeSpan.FromMinutes(15),
    };

    [SkippableFact]
    public async Task Picks_smallest_sufficient_resource()
    {
        Skip.IfNot(_pg.Available, "No Postgres available.");
        var biz = await _pg.SeedBusinessAsync(2, 4, 8);
        try
        {
            // capacities [2,4,8] → a party of 3 should take the 4-top, not the 8-top.
            var result = await _sut.CreateBookingHoldAsync(Request(biz, 3));

            Assert.True(result.Success);
            Assert.Equal(biz.ResourceIds[1], result.ResourceId);
            Assert.NotNull(result.HoldExpiresAt);
        }
        finally
        {
            await _pg.CleanupAsync(biz.BusinessId);
        }
    }

    [SkippableFact]
    public async Task Second_overlapping_hold_finds_no_availability()
    {
        Skip.IfNot(_pg.Available, "No Postgres available.");
        var biz = await _pg.SeedBusinessAsync(1); // single seat
        try
        {
            var first = await _sut.CreateBookingHoldAsync(Request(biz, 1));
            var second = await _sut.CreateBookingHoldAsync(Request(biz, 1));

            Assert.True(first.Success);
            Assert.False(second.Success);
            Assert.Equal("No resource is available for the requested time.", second.FailureReason);
        }
        finally
        {
            await _pg.CleanupAsync(biz.BusinessId);
        }
    }

    [SkippableFact]
    public async Task Concurrent_holds_for_one_seat_yield_exactly_one_success()
    {
        Skip.IfNot(_pg.Available, "No Postgres available.");
        var biz = await _pg.SeedBusinessAsync(1); // single seat
        try
        {
            const int attempts = 16;
            var tasks = Enumerable.Range(0, attempts)
                .Select(_ => _sut.CreateBookingHoldAsync(Request(biz, 1)))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            Assert.Equal(1, results.Count(r => r.Success));
            Assert.Equal(attempts - 1, results.Count(r => !r.Success));
        }
        finally
        {
            await _pg.CleanupAsync(biz.BusinessId);
        }
    }

    [SkippableFact]
    public async Task Database_constraint_rejects_raw_overlapping_insert()
    {
        Skip.IfNot(_pg.Available, "No Postgres available.");
        var biz = await _pg.SeedBusinessAsync(1);
        try
        {
            await using var conn = new NpgsqlConnection(_pg.ConnectionString);
            await conn.OpenAsync();

            await InsertBookingAsync(conn, biz, biz.ResourceIds[0], SlotStart, SlotEnd, "Confirmed");

            // Overlapping window on the same resource must be rejected by the GiST constraint.
            var ex = await Assert.ThrowsAsync<PostgresException>(() =>
                InsertBookingAsync(conn, biz, biz.ResourceIds[0], SlotStart.AddMinutes(30), SlotEnd.AddMinutes(30), "Confirmed"));

            Assert.Equal(PostgresErrorCodes.ExclusionViolation, ex.SqlState);
        }
        finally
        {
            await _pg.CleanupAsync(biz.BusinessId);
        }
    }

    [SkippableFact]
    public async Task Guarded_transition_succeeds_once_then_fails()
    {
        Skip.IfNot(_pg.Available, "No Postgres available.");
        var biz = await _pg.SeedBusinessAsync(1);
        try
        {
            var hold = await _sut.CreateBookingHoldAsync(Request(biz, 1));
            Assert.True(hold.Success);

            var first = await _sut.TryTransitionStatusAsync(
                biz.BusinessId, hold.BookingId!.Value, BookingStatus.PendingDeposit, BookingStatus.Confirmed);
            var second = await _sut.TryTransitionStatusAsync(
                biz.BusinessId, hold.BookingId!.Value, BookingStatus.PendingDeposit, BookingStatus.Confirmed);

            Assert.True(first);
            Assert.False(second);
        }
        finally
        {
            await _pg.CleanupAsync(biz.BusinessId);
        }
    }

    private static Task InsertBookingAsync(
        NpgsqlConnection conn, SeededBusiness biz, Guid resourceId,
        DateTimeOffset start, DateTimeOffset end, string status) =>
        conn.ExecuteAsync(
            """
            INSERT INTO bookings
                ("Id","BusinessId","CustomerId","ResourceId","StartTime","EndTime","Status","CreatedAt")
            VALUES (@id, @businessId, @customerId, @resourceId, @start, @end, @status, now());
            """,
            new
            {
                id = Guid.NewGuid(),
                businessId = biz.BusinessId,
                customerId = biz.CustomerId,
                resourceId,
                start = start.UtcDateTime,
                end = end.UtcDateTime,
                status,
            });
}
