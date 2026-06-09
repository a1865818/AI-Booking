using System.Collections.Concurrent;
using System.Text.Json;
using GhedDay.Domain.Entities;
using GhedDay.Infrastructure.AI;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.AI.Tools;

namespace GhedDay.Infrastructure.Tests.Fakes;

/// <summary>Records tool invocations and returns a canned tool_result for the orchestrator loop test.</summary>
public sealed class RecordingToolHandler : IClaudeToolHandler
{
    private readonly string _cannedResult;
    public List<string> Invocations { get; } = [];

    public RecordingToolHandler(string cannedResult = "{\"ok\":true}") => _cannedResult = cannedResult;

    public IReadOnlyList<ToolDefinition> GetDefinitions(Business business) => [];

    public Task<string> ExecuteAsync(string toolName, JsonElement input, ToolContext context, CancellationToken ct = default)
    {
        Invocations.Add(toolName);
        return Task.FromResult(_cannedResult);
    }
}

public sealed class InMemoryConversationStore : IConversationContextStore
{
    private readonly ConversationContext _context;
    public ConcurrentBag<string> Outbound { get; } = [];

    public InMemoryConversationStore(ConversationContext context) => _context = context;

    public Task<ConversationContext?> LoadAsync(Guid businessId, Guid conversationId, int historyLimit, CancellationToken ct = default) =>
        Task.FromResult<ConversationContext?>(_context);

    public Task AppendOutboundAsync(Guid businessId, Guid conversationId, string body, CancellationToken ct = default)
    {
        Outbound.Add(body);
        return Task.CompletedTask;
    }

    public static ConversationContext Context(bool optedOut = false, string? twilioNumber = "+15550000000") =>
        new(
            new Business { Id = Guid.NewGuid(), Name = "Test", Timezone = "UTC", TwilioNumber = twilioNumber },
            new Customer { Id = Guid.NewGuid(), PhoneE164 = "+14155550123", OptedOut = optedOut },
            [ClaudeMessage.User("hi")]);
}
