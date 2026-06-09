using GhedDay.Application.Services;
using GhedDay.Application.Verticals;

namespace GhedDay.Infrastructure.AI;

/// <summary>
/// Main Claude tool-use loop (Phase 2, critical path). Loads the business + vertical config,
/// builds the request, runs up to <c>MaxIterations</c> tool-use rounds, and sends the final
/// SMS. BusinessId/CustomerId are bound from the tenant context, never from tool args
/// (non-negotiable rule 1).
/// </summary>
public sealed class ClaudeConversationOrchestrator : IConversationOrchestrator
{
    private readonly ClaudeHttpClient _claude;
    private readonly IVerticalConfigService _verticalConfig;

    public ClaudeConversationOrchestrator(ClaudeHttpClient claude, IVerticalConfigService verticalConfig)
    {
        _claude = claude;
        _verticalConfig = verticalConfig;
    }

    public Task HandleInboundMessageAsync(
        Guid businessId,
        Guid conversationId,
        string inboundBody,
        CancellationToken ct = default)
    {
        // Phase 2: build request (system prompt + tools per vertical) → POST → dispatch
        // tool_use blocks via ClaudeToolHandler → loop → send outbound SMS + persist.
        throw new NotImplementedException("Claude tool-use loop is implemented in Phase 2.");
    }
}
