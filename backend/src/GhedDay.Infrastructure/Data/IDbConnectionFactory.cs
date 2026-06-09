using System.Data.Common;
using Npgsql;

namespace GhedDay.Infrastructure.Data;

/// <summary>
/// Opens raw ADO.NET connections for the Dapper + advisory-lock paths
/// (<c>CreateBookingHold</c>, <c>ClaimWaitlistSlot</c>, availability) that bypass EF
/// change-tracking by design (non-negotiable rule 2).
/// </summary>
public interface IDbConnectionFactory
{
    Task<DbConnection> OpenAsync(CancellationToken ct = default);
}

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(string connectionString) => _connectionString = connectionString;

    public async Task<DbConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
