using GhedDay.Domain.Enums;

namespace GhedDay.Domain.Events;

public interface IDomainEvent
{
    Guid BusinessId { get; }
    DateTimeOffset OccurredAt { get; }
}

public sealed record BookingCreatedEvent(Guid BusinessId, Guid BookingId, Guid CustomerId)
    : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record BookingConfirmedEvent(Guid BusinessId, Guid BookingId)
    : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record BookingCancelledEvent(Guid BusinessId, Guid BookingId, BookingStatus PreviousStatus)
    : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record EscalationRequestedEvent(Guid BusinessId, Guid ConversationId, string? CustomerName)
    : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
