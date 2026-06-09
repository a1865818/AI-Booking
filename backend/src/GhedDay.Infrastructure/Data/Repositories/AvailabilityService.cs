using Dapper;
using GhedDay.Application.DTOs;
using GhedDay.Application.Services;
using GhedDay.Domain.Enums;

namespace GhedDay.Infrastructure.Data.Repositories;

/// <summary>
/// Resource-capacity-aware availability. One raw SQL query covers every vertical; it always
/// filters by <c>business_id</c> (non-negotiable rule 3) and uses a tstzrange overlap test
/// against active holds/confirmations.
/// </summary>
public sealed class AvailabilityService : IAvailabilityService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AvailabilityService(IDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    private const string Sql = """
        SELECT r."Id"        AS ResourceId,
               r."Name"      AS ResourceName,
               r."Capacity"  AS Capacity
        FROM resources r
        WHERE r."BusinessId" = @businessId
          AND r."IsActive" = true
          AND r."Capacity" >= @requiredCapacity
          AND NOT EXISTS (
            SELECT 1 FROM bookings b
            WHERE b."ResourceId" = r."Id"
              AND b."Status" IN (@pendingDeposit, @confirmed)
              AND tstzrange(b."StartTime", b."EndTime") && tstzrange(@start, @end)
          )
        ORDER BY r."SortOrder", r."Name";
        """;

    public async Task<IReadOnlyList<SlotDto>> GetAvailableSlotsAsync(
        Guid businessId,
        DateTimeOffset start,
        DateTimeOffset end,
        int requiredCapacity,
        CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);

        var rows = await conn.QueryAsync<(Guid ResourceId, string ResourceName, int Capacity)>(
            new CommandDefinition(Sql, new
            {
                businessId,
                requiredCapacity,
                start,
                end,
                pendingDeposit = BookingStatus.PendingDeposit.ToString(),
                confirmed = BookingStatus.Confirmed.ToString()
            }, cancellationToken: ct));

        return rows
            .Select(r => new SlotDto(r.ResourceId, r.ResourceName, r.Capacity, start, end))
            .ToList();
    }
}
