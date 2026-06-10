using GhedDay.Api.Middleware;
using GhedDay.Application.Common;
using GhedDay.Application.DTOs;
using GhedDay.Application.Services;
using GhedDay.Application.Waitlist;
using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Domain.ValueObjects;
using GhedDay.Infrastructure.Configuration;
using GhedDay.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Twilio.Security;

namespace GhedDay.Api.Webhooks;

/// <summary>
/// Inbound SMS webhook. Order is mandatory (non-negotiable rules 1, 3, 4):
///   1. Validate the Twilio signature BEFORE any DB touch.
///   2. Insert into processed_events FIRST; a unique violation → 200 no-op.
///   3. Resolve the tenant from the receiving number server-side (never from the body), then
///      bind it into the request's ITenantContext so all downstream queries are scoped.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("webhooks/twilio")]
public sealed class TwilioWebhookController : ControllerBase
{
    private static readonly HashSet<string> OptOutKeywords =
        new(StringComparer.OrdinalIgnoreCase) { "STOP", "STOPALL", "UNSUBSCRIBE", "CANCEL", "END", "QUIT", "HỦY", "HUY" };

    private static readonly HashSet<string> OptInKeywords =
        new(StringComparer.OrdinalIgnoreCase) { "START", "UNSTOP" };

    private readonly GhedDayDbContext _db;
    private readonly IQueryFilterDisabler _filterDisabler;
    private readonly ITenantContext _tenantContext;
    private readonly IConversationOrchestrator _orchestrator;
    private readonly IWaitlistOfferService _waitlist;
    private readonly INotificationService _notifications;
    private readonly TwilioOptions _twilio;
    private readonly ILogger<TwilioWebhookController> _logger;

    public TwilioWebhookController(
        GhedDayDbContext db,
        IQueryFilterDisabler filterDisabler,
        ITenantContext tenantContext,
        IConversationOrchestrator orchestrator,
        IWaitlistOfferService waitlist,
        INotificationService notifications,
        IOptions<TwilioOptions> twilio,
        ILogger<TwilioWebhookController> logger)
    {
        _db = db;
        _filterDisabler = filterDisabler;
        _tenantContext = tenantContext;
        _orchestrator = orchestrator;
        _waitlist = waitlist;
        _notifications = notifications;
        _twilio = twilio.Value;
        _logger = logger;
    }

    [HttpPost("sms")]
    public async Task<IActionResult> Sms(CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);

        // 1. Signature validation before any DB touch.
        if (!IsValidSignature(form))
        {
            _logger.LogWarning("Rejected Twilio webhook with invalid signature.");
            return Forbid();
        }

        var messageSid = form["MessageSid"].ToString();
        var fromRaw = form["From"].ToString();
        var toRaw = form["To"].ToString();
        var body = form["Body"].ToString();

        if (string.IsNullOrWhiteSpace(messageSid) || string.IsNullOrWhiteSpace(fromRaw) || string.IsNullOrWhiteSpace(toRaw))
            return BadRequest();

        // 2. Idempotency guard — insert first; unique violation means already processed.
        _db.ProcessedEvents.Add(new ProcessedEvent { Id = messageSid, Source = "twilio" });
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _logger.LogInformation("Duplicate Twilio delivery {MessageSid}; returning 200 no-op.", messageSid);
            return Ok();
        }

        // 3. Resolve the tenant from the receiving number (cross-tenant lookup; no tenant yet).
        var business = await _filterDisabler.RunWithoutTenantFilterAsync(
            () => _db.Businesses.FirstOrDefaultAsync(b => b.TwilioNumber == toRaw, ct), ct);

        if (business is null)
        {
            _logger.LogWarning("Inbound SMS to unprovisioned number {To}; ignoring.", toRaw);
            return Ok();
        }

        // Bind the tenant so every downstream query is scoped (rules 1 & 3).
        if (_tenantContext is TenantContext concrete)
            concrete.Set(business.Id, isSuperAdmin: false);

        if (!PhoneNumber.TryParse(fromRaw, out var phone) || phone is null)
        {
            _logger.LogWarning("Inbound SMS from unparseable number {From}; ignoring.", fromRaw);
            return Ok();
        }

        var customer = await UpsertCustomerAsync(business.Id, phone.Value, ct);
        var conversation = await GetOrCreateConversationAsync(business.Id, customer.Id, ct);

        await PersistInboundAsync(business.Id, conversation.Id, body, ct);
        await _notifications.NewConversationMessageAsync(business.Id, conversation.Id, new MessageDto(
            Guid.NewGuid(), conversation.Id, MessageDirection.Inbound, body, DateTimeOffset.UtcNow), ct);

        // Opt-out / opt-in keywords short-circuit the AI.
        var trimmed = body.Trim();
        if (OptOutKeywords.Contains(trimmed))
        {
            await SetOptOutAsync(customer.Id, true, ct);
            return Ok();
        }
        if (OptInKeywords.Contains(trimmed))
        {
            await SetOptOutAsync(customer.Id, false, ct);
        }

        if (await _waitlist.TryAcceptOfferReplyAsync(business.Id, customer.Id, trimmed, ct))
            return Ok();

        if (customer.OptedOut || !conversation.AiEnabled)
        {
            _logger.LogInformation("AI skipped for conversation {ConversationId} (optedOut={OptedOut}, aiEnabled={AiEnabled}).",
                conversation.Id, customer.OptedOut, conversation.AiEnabled);
            return Ok();
        }

        await _orchestrator.HandleInboundMessageAsync(business.Id, conversation.Id, body, ct);
        return Ok();
    }

    private async Task<Customer> UpsertCustomerAsync(Guid businessId, string phoneE164, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.PhoneE164 == phoneE164, ct);
        if (customer is not null)
            return customer;

        customer = new Customer { BusinessId = businessId, PhoneE164 = phoneE164 };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
        return customer;
    }

    private async Task<Conversation> GetOrCreateConversationAsync(Guid businessId, Guid customerId, CancellationToken ct)
    {
        var conversation = await _db.Conversations
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        if (conversation is not null)
            return conversation;

        conversation = new Conversation { BusinessId = businessId, CustomerId = customerId };
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync(ct);
        return conversation;
    }

    private async Task PersistInboundAsync(Guid businessId, Guid conversationId, string body, CancellationToken ct)
    {
        _db.Messages.Add(new Message
        {
            ConversationId = conversationId,
            BusinessId = businessId,
            Direction = MessageDirection.Inbound,
            Body = body,
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task SetOptOutAsync(Guid customerId, bool optedOut, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, ct);
        if (customer is null)
            return;
        customer.OptedOut = optedOut;
        await _db.SaveChangesAsync(ct);
    }

    private bool IsValidSignature(IFormCollection form)
    {
        if (string.IsNullOrWhiteSpace(_twilio.AuthToken))
            return false;

        var signature = Request.Headers["X-Twilio-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        var validator = new RequestValidator(_twilio.AuthToken);
        var url = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";
        var parameters = form.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        return validator.Validate(url, parameters, signature);
    }
}
