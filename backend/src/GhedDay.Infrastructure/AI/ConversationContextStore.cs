using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Infrastructure.AI;

public sealed record ConversationContext(
    Business Business,
    Customer Customer,
    IReadOnlyList<ClaudeMessage> History);

/// <summary>
/// Loads the business + customer + recent transcript for a conversation and persists outbound
/// replies. All reads/writes flow through the tenant-scoped DbContext (rule 3).
/// </summary>
public interface IConversationContextStore
{
    Task<ConversationContext?> LoadAsync(Guid businessId, Guid conversationId, int historyLimit, CancellationToken ct = default);

    Task AppendOutboundAsync(Guid businessId, Guid conversationId, string body, CancellationToken ct = default);
}

public sealed class ConversationContextStore : IConversationContextStore
{
    private readonly GhedDayDbContext _db;

    public ConversationContextStore(GhedDayDbContext db) => _db = db;

    public async Task<ConversationContext?> LoadAsync(
        Guid businessId, Guid conversationId, int historyLimit, CancellationToken ct = default)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, ct);
        if (business is null)
            return null;

        var conversation = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);
        if (conversation is null)
            return null;

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == conversation.CustomerId, ct);
        if (customer is null)
            return null;

        var recent = await _db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(historyLimit)
            .ToListAsync(ct);

        recent.Reverse();
        var history = ToAlternatingMessages(recent);

        return new ConversationContext(business, customer, history);
    }

    public async Task AppendOutboundAsync(Guid businessId, Guid conversationId, string body, CancellationToken ct = default)
    {
        _db.Messages.Add(new Message
        {
            ConversationId = conversationId,
            BusinessId = businessId,
            Direction = MessageDirection.Outbound,
            Body = body,
        });

        var conversation = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);
        if (conversation is not null)
            conversation.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Maps stored messages to Claude turns, merging consecutive same-direction messages and
    /// trimming any leading assistant turn so the transcript starts with a user message and
    /// alternates — both required by the Messages API.
    /// </summary>
    private static List<ClaudeMessage> ToAlternatingMessages(IReadOnlyList<Message> messages)
    {
        var result = new List<ClaudeMessage>();

        foreach (var message in messages)
        {
            var role = message.Direction == MessageDirection.Inbound ? "user" : "assistant";

            // Skip a leading assistant turn — the conversation must open with the customer.
            if (result.Count == 0 && role == "assistant")
                continue;

            if (result.Count > 0 && result[^1].Role == role)
            {
                result[^1].Content[0].Text += "\n" + message.Body;
                continue;
            }

            result.Add(role == "user" ? ClaudeMessage.User(message.Body) : ClaudeMessage.Assistant(message.Body));
        }

        return result;
    }
}
