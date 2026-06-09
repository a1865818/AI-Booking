using Dapper;
using GhedDay.Infrastructure.Data;
using Npgsql;

namespace GhedDay.Infrastructure.Tests;

/// <summary>
/// Shared Postgres connection for integration tests. The connection string comes from
/// <c>GHEDDAY_TEST_CONNECTION</c> (set by CI's Postgres service) and falls back to the local
/// dev database. Tests are skipped when no database is reachable so unit-only environments
/// stay green.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("GHEDDAY_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=ghedday;Username=postgres;Password=postgres";

    public bool Available { get; private set; }

    public NpgsqlConnectionFactory ConnectionFactory => new(ConnectionString);

    public async Task InitializeAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            // The migrations (incl. the overlap constraint) are applied by the API on dev
            // startup / by CI before tests run; just confirm the schema is present.
            var exists = await conn.ExecuteScalarAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'bookings');");
            Available = exists;
        }
        catch
        {
            Available = false;
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Creates a business with one customer and the given resource capacities; returns ids for
    /// assertions and to satisfy the bookings foreign keys.
    /// </summary>
    public async Task<SeededBusiness> SeedBusinessAsync(params int[] capacities)
    {
        var businessId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        await conn.ExecuteAsync(
            """
            INSERT INTO businesses ("Id", "Name", "Slug", "Timezone", "BusinessType", vertical_config, "CreatedAt")
            VALUES (@id, @name, @slug, 'UTC', 'Other', '{}'::jsonb, now());
            """,
            new { id = businessId, name = $"Test {businessId:N}", slug = businessId.ToString("N") });

        await conn.ExecuteAsync(
            """
            INSERT INTO customers ("Id", "BusinessId", "PhoneE164", "LanguagePref", "OptedOut", "CreatedAt")
            VALUES (@id, @businessId, @phone, 'en', false, now());
            """,
            new { id = customerId, businessId, phone = $"+1{Random.Shared.NextInt64(1000000000, 9999999999)}" });

        var resourceIds = new Guid[capacities.Length];
        for (var i = 0; i < capacities.Length; i++)
        {
            var rid = Guid.NewGuid();
            resourceIds[i] = rid;
            await conn.ExecuteAsync(
                """
                INSERT INTO resources ("Id", "BusinessId", "Name", "ResourceType", "Capacity", "IsActive", "SortOrder")
                VALUES (@id, @businessId, @name, 'Table', @capacity, true, @sort);
                """,
                new { id = rid, businessId, name = $"R{i + 1}", capacity = capacities[i], sort = i });
        }

        return new SeededBusiness(businessId, customerId, resourceIds);
    }

    public async Task CleanupAsync(Guid businessId)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync("DELETE FROM bookings WHERE \"BusinessId\" = @businessId;", new { businessId });
        await conn.ExecuteAsync("DELETE FROM resources WHERE \"BusinessId\" = @businessId;", new { businessId });
        await conn.ExecuteAsync("DELETE FROM customers WHERE \"BusinessId\" = @businessId;", new { businessId });
        await conn.ExecuteAsync("DELETE FROM businesses WHERE \"Id\" = @businessId;", new { businessId });
    }
}

public sealed record SeededBusiness(Guid BusinessId, Guid CustomerId, Guid[] ResourceIds);

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
