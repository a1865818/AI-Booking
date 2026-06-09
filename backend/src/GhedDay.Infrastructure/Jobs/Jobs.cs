using Dapper;
using GhedDay.Infrastructure.Data;

namespace GhedDay.Infrastructure.Jobs;

/// <summary>
/// Prunes processed_events older than the retention window so the idempotency table does not
/// grow unbounded. The dedupe guarantee only needs to cover the providers' retry windows.
/// </summary>
public sealed class ProcessedEventCleanupJob
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    private readonly IDbConnectionFactory _connectionFactory;

    public ProcessedEventCleanupJob(IDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM processed_events WHERE \"ProcessedAt\" < @cutoff;",
            new { cutoff = DateTimeOffset.UtcNow - Retention }, cancellationToken: ct));
    }
}

/// <summary>Sweeps pending_deposit bookings past hold_expires_at → cancel → waitlist check. (Phase 4.)</summary>
public sealed class HoldExpiryJob
{
    public Task RunAsync(CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Sends 24h + 1h SMS reminders for confirmed bookings. (Phase 4.)</summary>
public sealed class ReminderJob
{
    public Task RunAsync(CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Expires unaccepted waitlist offers and offers the slot to the next entry. (Phase 4.)</summary>
public sealed class WaitlistOfferTimeoutJob
{
    public Task RunAsync(CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Auto-marks no-show 15 minutes after end_time. (Phase 4.)</summary>
public sealed class NoShowJob
{
    public Task RunAsync(CancellationToken ct = default) => Task.CompletedTask;
}
