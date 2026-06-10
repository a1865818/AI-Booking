using Dapper;
using GhedDay.Application.Bookings;
using GhedDay.Application.Services;
using GhedDay.Application.Waitlist;
using GhedDay.Domain.Enums;
using GhedDay.Infrastructure.Data;
using Microsoft.Extensions.Logging;

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

/// <summary>Sweeps pending_deposit bookings past hold_expires_at → cancel → waitlist check.</summary>
public sealed class HoldExpiryJob
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IBookingRepository _bookings;
    private readonly IWaitlistOfferService _waitlist;
    private readonly INotificationService _notifications;
    private readonly ILogger<HoldExpiryJob> _logger;

    public HoldExpiryJob(
        IDbConnectionFactory connectionFactory,
        IBookingRepository bookings,
        IWaitlistOfferService waitlist,
        INotificationService notifications,
        ILogger<HoldExpiryJob> logger)
    {
        _connectionFactory = connectionFactory;
        _bookings = bookings;
        _waitlist = waitlist;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);

        var expired = (await conn.QueryAsync<ExpiredHoldRow>(new CommandDefinition(
            """
            SELECT "Id" AS Id,
                   "BusinessId" AS BusinessId,
                   "StartTime" AS StartTime,
                   "EndTime" AS EndTime,
                   "PartySize" AS PartySize,
                   "OfferingId" AS OfferingId
            FROM bookings
            WHERE "Status" = @pending
              AND "HoldExpiresAt" IS NOT NULL
              AND "HoldExpiresAt" < @now;
            """,
            new
            {
                pending = BookingStatus.PendingDeposit.ToString(),
                now = DateTimeOffset.UtcNow.UtcDateTime,
            },
            cancellationToken: ct))).ToList();

        foreach (var row in expired)
        {
            var cancelled = await _bookings.TryTransitionStatusAsync(
                row.BusinessId, row.Id, BookingStatus.PendingDeposit, BookingStatus.Cancelled, ct);

            if (!cancelled)
                continue;

            await _notifications.BookingStatusChangedAsync(
                row.BusinessId, row.Id, BookingStatus.Cancelled.ToString(), ct);

            await _waitlist.TryOfferReleasedSlotAsync(
                row.BusinessId, row.StartTime, row.EndTime, row.PartySize, row.OfferingId, ct);

            _logger.LogInformation("Expired hold on booking {BookingId}; waitlist notified.", row.Id);
        }
    }

    private sealed record ExpiredHoldRow(
        Guid Id,
        Guid BusinessId,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        int? PartySize,
        Guid? OfferingId);
}

/// <summary>Sends 24h + 1h SMS reminders for confirmed bookings.</summary>
public sealed class ReminderJob
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISmsService _sms;
    private readonly ILogger<ReminderJob> _logger;

    public ReminderJob(
        IDbConnectionFactory connectionFactory,
        ISmsService sms,
        ILogger<ReminderJob> logger)
    {
        _connectionFactory = connectionFactory;
        _sms = sms;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        await SendDueRemindersAsync(ReminderType.TwentyFourHour, TimeSpan.FromHours(24), ct);
        await SendDueRemindersAsync(ReminderType.OneHour, TimeSpan.FromHours(1), ct);
    }

    private async Task SendDueRemindersAsync(ReminderType type, TimeSpan leadTime, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now + leadTime - TimeSpan.FromMinutes(10);
        var windowEnd = now + leadTime + TimeSpan.FromMinutes(10);

        await using var conn = await _connectionFactory.OpenAsync(ct);

        var due = (await conn.QueryAsync<ReminderCandidate>(new CommandDefinition(
            """
            SELECT b."Id" AS BookingId,
                   b."BusinessId" AS BusinessId,
                   b."StartTime" AS StartTime,
                   c."PhoneE164" AS PhoneE164,
                   bus."TwilioNumber" AS TwilioNumber,
                   bus."Name" AS BusinessName
            FROM bookings b
            INNER JOIN customers c ON c."Id" = b."CustomerId" AND c."BusinessId" = b."BusinessId"
            INNER JOIN businesses bus ON bus."Id" = b."BusinessId"
            WHERE b."Status" = @confirmed
              AND b."StartTime" >= @windowStart
              AND b."StartTime" <= @windowEnd
              AND bus."TwilioNumber" IS NOT NULL
              AND NOT EXISTS (
                SELECT 1 FROM reminders r
                WHERE r."BookingId" = b."Id"
                  AND r."Type" = @type
                  AND r."SentAt" IS NOT NULL
              );
            """,
            new
            {
                confirmed = BookingStatus.Confirmed.ToString(),
                windowStart = windowStart.UtcDateTime,
                windowEnd = windowEnd.UtcDateTime,
                type = type.ToString(),
            },
            cancellationToken: ct))).ToList();

        foreach (var row in due)
        {
            var label = type == ReminderType.TwentyFourHour ? "tomorrow" : "in about an hour";
            var labelVi = type == ReminderType.TwentyFourHour ? "ngày mai" : "khoảng 1 giờ nữa";
            var body =
                $"Reminder: your booking at {row.BusinessName} is {label} ({row.StartTime:ddd h:mm tt} UTC). " +
                $"Nhắc nhở: lịch hẹn tại {row.BusinessName} là {labelVi}.";

            try
            {
                await _sms.SendAsync(row.PhoneE164, row.TwilioNumber!, body, ct);

                await conn.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO reminders ("Id", "BookingId", "BusinessId", "Type", "ScheduledFor", "SentAt")
                    VALUES (@id, @bookingId, @businessId, @type, @scheduledFor, @sentAt);
                    """,
                    new
                    {
                        id = Guid.NewGuid(),
                        bookingId = row.BookingId,
                        businessId = row.BusinessId,
                        type = type.ToString(),
                        scheduledFor = row.StartTime.UtcDateTime,
                        sentAt = DateTimeOffset.UtcNow.UtcDateTime,
                    },
                    cancellationToken: ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {Type} reminder for booking {BookingId}.", type, row.BookingId);
            }
        }
    }

    private sealed record ReminderCandidate(
        Guid BookingId,
        Guid BusinessId,
        DateTimeOffset StartTime,
        string PhoneE164,
        string? TwilioNumber,
        string BusinessName);
}

/// <summary>Expires unaccepted waitlist offers and offers the slot to the next entry.</summary>
public sealed class WaitlistOfferTimeoutJob
{
    private readonly IWaitlistOfferService _waitlist;

    public WaitlistOfferTimeoutJob(IWaitlistOfferService waitlist) => _waitlist = waitlist;

    public Task RunAsync(CancellationToken ct = default) => _waitlist.ProcessExpiredOffersAsync(ct);
}

/// <summary>Auto-marks no-show 15 minutes after end_time.</summary>
public sealed class NoShowJob
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IBookingRepository _bookings;
    private readonly INotificationService _notifications;
    private readonly ILogger<NoShowJob> _logger;

    public NoShowJob(
        IDbConnectionFactory connectionFactory,
        IBookingRepository bookings,
        INotificationService notifications,
        ILogger<NoShowJob> logger)
    {
        _connectionFactory = connectionFactory;
        _bookings = bookings;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(15);

        await using var conn = await _connectionFactory.OpenAsync(ct);

        var overdue = (await conn.QueryAsync<NoShowCandidate>(new CommandDefinition(
            """
            SELECT "Id" AS Id, "BusinessId" AS BusinessId
            FROM bookings
            WHERE "Status" = @confirmed
              AND "EndTime" < @cutoff;
            """,
            new
            {
                confirmed = BookingStatus.Confirmed.ToString(),
                cutoff = cutoff.UtcDateTime,
            },
            cancellationToken: ct))).ToList();

        foreach (var row in overdue)
        {
            var marked = await _bookings.TryTransitionStatusAsync(
                row.BusinessId, row.Id, BookingStatus.Confirmed, BookingStatus.NoShow, ct);

            if (marked)
            {
                await _notifications.BookingStatusChangedAsync(
                    row.BusinessId, row.Id, BookingStatus.NoShow.ToString(), ct);
                _logger.LogInformation("Marked booking {BookingId} as no-show.", row.Id);
            }
        }
    }

    private sealed record NoShowCandidate(Guid Id, Guid BusinessId);
}
