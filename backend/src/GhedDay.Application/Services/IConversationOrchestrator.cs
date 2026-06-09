namespace GhedDay.Application.Services;

/// <summary>
/// Drives the Claude tool-use loop for one inbound message. Implemented in Infrastructure
/// (<c>ClaudeConversationOrchestrator</c>). BusinessId/CustomerId are bound from the resolved
/// tenant context, never from tool arguments (non-negotiable rule 1).
/// </summary>
public interface IConversationOrchestrator
{
    Task HandleInboundMessageAsync(
        Guid businessId,
        Guid conversationId,
        string inboundBody,
        CancellationToken ct = default);
}
