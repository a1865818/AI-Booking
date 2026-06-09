using GhedDay.Domain.Enums;

namespace GhedDay.Domain.Entities;

public class Reminder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }

    /// <summary>Denormalised tenant key for the global query filter.</summary>
    public Guid BusinessId { get; set; }

    public ReminderType Type { get; set; }
    public DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset? SentAt { get; set; }

    public Booking? Booking { get; set; }
}
