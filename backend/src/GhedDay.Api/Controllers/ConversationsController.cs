using GhedDay.Application.Common;
using GhedDay.Application.DTOs;
using GhedDay.Application.Services;
using GhedDay.Domain.Enums;
using GhedDay.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ConversationsController : ControllerBase
{
    private readonly GhedDayDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ISmsService _sms;
    private readonly INotificationService _notifications;

    public ConversationsController(
        GhedDayDbContext db,
        ITenantContext tenant,
        ISmsService sms,
        INotificationService notifications)
    {
        _db = db;
        _tenant = tenant;
        _sms = sms;
        _notifications = notifications;
    }

    /// <summary>Conversation list for the current tenant with a last-message preview.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var conversations = await _db.Conversations
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new
            {
                id = c.Id,
                customerName = c.Customer!.Name ?? c.Customer.PhoneE164,
                status = c.Status.ToString(),
                aiEnabled = c.AiEnabled,
                escalated = c.Status == ConversationStatus.HumanTakeover,
                updatedAt = c.UpdatedAt,
                preview = _db.Messages
                    .Where(m => m.ConversationId == c.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Body)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        return Ok(conversations);
    }

    /// <summary>A single conversation transcript.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var conversation = await _db.Conversations
            .Where(c => c.Id == id)
            .Select(c => new
            {
                id = c.Id,
                customerName = c.Customer!.Name ?? c.Customer.PhoneE164,
                aiEnabled = c.AiEnabled,
                status = c.Status.ToString(),
            })
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
            return NotFound();

        var messages = await _db.Messages
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                id = m.Id,
                conversationId = m.ConversationId,
                direction = m.Direction.ToString(),
                body = m.Body,
                createdAt = m.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(new { conversation, messages });
    }

    /// <summary>Pause or resume AI for a conversation (human takeover handoff).</summary>
    [HttpPost("{id:guid}/toggle-ai")]
    public async Task<IActionResult> ToggleAi(Guid id, [FromBody] ToggleAiRequest request, CancellationToken ct)
    {
        var businessId = _tenant.RequireBusinessId();
        var conversation = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (conversation is null)
            return NotFound();

        conversation.AiEnabled = request.Enabled;
        conversation.Status = request.Enabled ? ConversationStatus.Active : ConversationStatus.HumanTakeover;
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _notifications.AiToggledAsync(businessId, id, request.Enabled, ct);

        return Ok(new { conversation.Id, conversation.AiEnabled, status = conversation.Status.ToString() });
    }

    /// <summary>Send an outbound SMS as the business owner (human reply).</summary>
    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { error = "Message body is required." });

        var businessId = _tenant.RequireBusinessId();
        var conversation = await _db.Conversations
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (conversation?.Customer is null)
            return NotFound();

        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, ct);
        if (business?.TwilioNumber is null)
            return BadRequest(new { error = "Business has no SMS number configured." });

        await _sms.SendAsync(
            conversation.Customer.PhoneE164,
            business.TwilioNumber,
            request.Body.Trim(),
            ct);

        var message = new Domain.Entities.Message
        {
            ConversationId = id,
            BusinessId = businessId,
            Direction = MessageDirection.Outbound,
            Body = request.Body.Trim(),
        };
        _db.Messages.Add(message);
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var dto = new MessageDto(
            message.Id, id, MessageDirection.Outbound, message.Body, message.CreatedAt);
        await _notifications.NewConversationMessageAsync(businessId, id, dto, ct);

        return Ok(new
        {
            id = message.Id,
            conversationId = id,
            direction = message.Direction.ToString(),
            body = message.Body,
            createdAt = message.CreatedAt,
        });
    }

    public sealed record ToggleAiRequest(bool Enabled);
    public sealed record SendMessageRequest(string Body);
}
