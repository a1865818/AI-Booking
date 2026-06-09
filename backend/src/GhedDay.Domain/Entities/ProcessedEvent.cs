namespace GhedDay.Domain.Entities;

/// <summary>
/// Idempotency guard for inbound webhooks. Composite PK (Id, Source). The webhook handler
/// inserts a row FIRST; a unique violation means the event was already processed (rule 4).
/// </summary>
public class ProcessedEvent
{
    /// <summary>External event id — Twilio MessageSid or Stripe event id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>'twilio' | 'stripe'.</summary>
    public string Source { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
