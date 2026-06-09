using GhedDay.Application.DTOs;
using GhedDay.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace GhedDay.Api.Hubs;

/// <summary>SignalR-backed implementation of the notification abstraction (PLAN §2.7).</summary>
public sealed class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<BookingHub> _hub;

    public SignalRNotificationService(IHubContext<BookingHub> hub) => _hub = hub;

    private IClientProxy Group(Guid businessId) => _hub.Clients.Group(BookingHub.GroupName(businessId));

    public Task BookingCreatedAsync(Guid businessId, BookingDto booking, CancellationToken ct = default) =>
        Group(businessId).SendAsync("BookingCreated", new { booking }, ct);

    public Task BookingStatusChangedAsync(Guid businessId, Guid bookingId, string newStatus, CancellationToken ct = default) =>
        Group(businessId).SendAsync("BookingStatusChanged", new { bookingId, newStatus }, ct);

    public Task NewConversationMessageAsync(Guid businessId, Guid conversationId, MessageDto message, CancellationToken ct = default) =>
        Group(businessId).SendAsync("NewConversationMessage", new { conversationId, message }, ct);

    public Task EscalationRequiredAsync(Guid businessId, Guid conversationId, string? customerName, CancellationToken ct = default) =>
        Group(businessId).SendAsync("EscalationRequired", new { conversationId, customerName }, ct);

    public Task AiToggledAsync(Guid businessId, Guid conversationId, bool aiEnabled, CancellationToken ct = default) =>
        Group(businessId).SendAsync("AiToggled", new { conversationId, aiEnabled }, ct);
}
