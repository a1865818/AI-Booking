using GhedDay.Application.DTOs;

namespace GhedDay.Application.Services;

/// <summary>
/// Abstraction over SignalR dispatch. All events are scoped to group
/// <c>business_{businessId}</c> (see PLAN §2.7).
/// </summary>
public interface INotificationService
{
    Task BookingCreatedAsync(Guid businessId, BookingDto booking, CancellationToken ct = default);
    Task BookingStatusChangedAsync(Guid businessId, Guid bookingId, string newStatus, CancellationToken ct = default);
    Task NewConversationMessageAsync(Guid businessId, Guid conversationId, MessageDto message, CancellationToken ct = default);
    Task EscalationRequiredAsync(Guid businessId, Guid conversationId, string? customerName, CancellationToken ct = default);
    Task AiToggledAsync(Guid businessId, Guid conversationId, bool aiEnabled, CancellationToken ct = default);
}
