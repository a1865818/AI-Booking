namespace GhedDay.Infrastructure.Jobs;

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
