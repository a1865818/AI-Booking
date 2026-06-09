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

    public ConversationsController(GhedDayDbContext db) => _db = db;

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
}
