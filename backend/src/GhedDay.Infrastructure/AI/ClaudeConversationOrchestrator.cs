using System.Text.Json;
using GhedDay.Application.DTOs;
using GhedDay.Application.Services;
using GhedDay.Domain.Enums;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.AI.Tools;
using GhedDay.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GhedDay.Infrastructure.AI;

/// <summary>
/// The Claude tool-use loop (PLAN §2.5). For one inbound message it loads context, runs up to
/// <c>MaxIterations</c> tool rounds, then sends + persists the reply and pushes the SignalR
/// event. <c>BusinessId</c>/<c>CustomerId</c> are bound from the resolved context and passed to
/// tools via <see cref="ToolContext"/> — never sourced from tool arguments (rule 1).
/// </summary>
public sealed class ClaudeConversationOrchestrator : IConversationOrchestrator
{
    private const int HistoryLimit = 20;

    private const string FallbackReply =
        "Sorry, I'm having trouble right now — a team member will follow up shortly. " +
        "Xin lỗi, hiện tại tôi đang gặp sự cố — nhân viên sẽ liên hệ với bạn sớm.";

    private static readonly JsonElement EmptyInput = JsonDocument.Parse("{}").RootElement.Clone();

    private readonly IClaudeClient _claude;
    private readonly ClaudeRequestBuilder _requestBuilder;
    private readonly IClaudeToolHandler _toolHandler;
    private readonly IConversationContextStore _store;
    private readonly ISmsService _sms;
    private readonly INotificationService _notifications;
    private readonly AnthropicOptions _options;
    private readonly ILogger<ClaudeConversationOrchestrator> _logger;

    public ClaudeConversationOrchestrator(
        IClaudeClient claude,
        ClaudeRequestBuilder requestBuilder,
        IClaudeToolHandler toolHandler,
        IConversationContextStore store,
        ISmsService sms,
        INotificationService notifications,
        IOptions<AnthropicOptions> options,
        ILogger<ClaudeConversationOrchestrator> logger)
    {
        _claude = claude;
        _requestBuilder = requestBuilder;
        _toolHandler = toolHandler;
        _store = store;
        _sms = sms;
        _notifications = notifications;
        _options = options.Value;
        _logger = logger;
    }

    public async Task HandleInboundMessageAsync(
        Guid businessId, Guid conversationId, string inboundBody, CancellationToken ct = default)
    {
        var context = await _store.LoadAsync(businessId, conversationId, HistoryLimit, ct);
        if (context is null)
        {
            _logger.LogWarning("No context for conversation {ConversationId} in business {BusinessId}", conversationId, businessId);
            return;
        }

        if (context.Customer.OptedOut)
        {
            _logger.LogInformation("Customer {CustomerId} has opted out; skipping AI reply.", context.Customer.Id);
            return;
        }

        var messages = new List<ClaudeMessage>(context.History);
        if (messages.Count == 0)
            messages.Add(ClaudeMessage.User(inboundBody));

        var toolContext = new ToolContext(context.Business, context.Customer);
        var request = _requestBuilder.Build(context.Business, messages, _toolHandler.GetDefinitions(context.Business));

        var replyText = await RunToolLoopAsync(request, toolContext, ct);

        await DeliverReplyAsync(context, businessId, conversationId, replyText, ct);
    }

    private async Task<string> RunToolLoopAsync(ClaudeRequest request, ToolContext toolContext, CancellationToken ct)
    {
        var maxIterations = Math.Max(1, _options.MaxIterations);

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var response = await _claude.CreateMessageAsync(request, ct);

            // Replay the assistant turn so any tool_result blocks can reference its tool_use ids.
            request.Messages.Add(new ClaudeMessage { Role = "assistant", Content = response.Content });

            if (!response.RequiresToolUse)
                return response.ConcatenatedText();

            var toolResults = new List<ContentBlock>();
            foreach (var toolUse in response.ToolUses)
            {
                var result = await _toolHandler.ExecuteAsync(
                    toolUse.Name ?? string.Empty, toolUse.Input ?? EmptyInput, toolContext, ct);
                toolResults.Add(ContentBlock.ToolResult(toolUse.Id ?? string.Empty, result));
            }

            request.Messages.Add(new ClaudeMessage { Role = "user", Content = toolResults });
        }

        _logger.LogWarning("Claude loop exceeded {Max} iterations for business {BusinessId}; sending fallback.",
            maxIterations, toolContext.BusinessId);
        return FallbackReply;
    }

    private async Task DeliverReplyAsync(
        ConversationContext context, Guid businessId, Guid conversationId, string replyText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(replyText))
            replyText = FallbackReply;

        if (!string.IsNullOrWhiteSpace(context.Business.TwilioNumber))
        {
            try
            {
                await _sms.SendAsync(context.Customer.PhoneE164, context.Business.TwilioNumber!, replyText, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send outbound SMS for conversation {ConversationId}", conversationId);
            }
        }

        await _store.AppendOutboundAsync(businessId, conversationId, replyText, ct);

        await _notifications.NewConversationMessageAsync(businessId, conversationId, new MessageDto(
            Guid.NewGuid(), conversationId, MessageDirection.Outbound, replyText, DateTimeOffset.UtcNow), ct);
    }
}
