using System.Collections.Concurrent;
using System.Text.Json;
using GhedDay.Application.Common;
using GhedDay.Application.DTOs;
using GhedDay.Application.Services;
using GhedDay.Infrastructure.AI.Models;

namespace GhedDay.Infrastructure.Tests.Fakes;

/// <summary>Returns scripted Claude responses in order — no network. Used to drive the loop.</summary>
public sealed class ScriptedClaudeClient : IClaudeClient
{
    private readonly Queue<ClaudeResponse> _responses;
    public List<ClaudeRequest> Requests { get; } = [];

    public ScriptedClaudeClient(params ClaudeResponse[] responses) =>
        _responses = new Queue<ClaudeResponse>(responses);

    public Task<ClaudeResponse> CreateMessageAsync(ClaudeRequest request, CancellationToken ct = default)
    {
        Requests.Add(request);
        // Snapshot count to keep the loop honest if it runs longer than scripted.
        var response = _responses.Count > 0
            ? _responses.Dequeue()
            : ToolUse("never_ends", "loopy", new { }); // force fallback if over-scripted
        return Task.FromResult(response);
    }

    public static ClaudeResponse EndTurn(string text) => new()
    {
        StopReason = "end_turn",
        Content = [ContentBlock.TextBlock(text)],
    };

    public static ClaudeResponse ToolUse(string name, string id, object input) => new()
    {
        StopReason = "tool_use",
        Content =
        [
            new ContentBlock
            {
                Type = "tool_use",
                Id = id,
                Name = name,
                Input = JsonSerializer.SerializeToElement(input),
            },
        ],
    };
}

public sealed class CapturingSmsService : ISmsService
{
    public ConcurrentBag<(string To, string From, string Body)> Sent { get; } = [];

    public Task SendAsync(string toE164, string fromE164, string body, CancellationToken ct = default)
    {
        Sent.Add((toE164, fromE164, body));
        return Task.CompletedTask;
    }
}

public sealed class NullNotificationService : INotificationService
{
    public Task BookingCreatedAsync(Guid businessId, BookingDto booking, CancellationToken ct = default) => Task.CompletedTask;
    public Task BookingStatusChangedAsync(Guid businessId, Guid bookingId, string newStatus, CancellationToken ct = default) => Task.CompletedTask;
    public Task NewConversationMessageAsync(Guid businessId, Guid conversationId, MessageDto message, CancellationToken ct = default) => Task.CompletedTask;
    public Task EscalationRequiredAsync(Guid businessId, Guid conversationId, string? customerName, CancellationToken ct = default) => Task.CompletedTask;
    public Task AiToggledAsync(Guid businessId, Guid conversationId, bool aiEnabled, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class TenantStub : ITenantContext
{
    public TenantStub(Guid? businessId) => BusinessId = businessId;
    public Guid? BusinessId { get; }
    public bool IsSuperAdmin => false;
    public Guid RequireBusinessId() => BusinessId ?? throw new InvalidOperationException("No tenant.");
}

/// <summary>A TimeProvider pinned to a fixed instant for deterministic prompts/availability.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
