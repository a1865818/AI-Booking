using GhedDay.Domain.Entities;
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
///   3. Resolve the tenant from the receiving number server-side (never from the body).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("webhooks/twilio")]
public sealed class TwilioWebhookController : ControllerBase
{
    private readonly GhedDayDbContext _db;
    private readonly TwilioOptions _twilio;
    private readonly ILogger<TwilioWebhookController> _logger;

    public TwilioWebhookController(
        GhedDayDbContext db,
        IOptions<TwilioOptions> twilio,
        ILogger<TwilioWebhookController> logger)
    {
        _db = db;
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
        if (string.IsNullOrWhiteSpace(messageSid))
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

        // 3. Tenant resolved from the receiving number server-side (Phase 2 hands off to the
        //    ConversationOrchestrator from here). BusinessId/CustomerId never come from the body.
        var toNumber = form["To"].ToString();
        _logger.LogInformation("Accepted inbound SMS {MessageSid} to {ToNumber}.", messageSid, toNumber);

        return Ok();
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
