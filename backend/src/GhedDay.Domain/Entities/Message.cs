using GhedDay.Domain.Enums;

namespace GhedDay.Domain.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }

    /// <summary>Denormalised tenant key so the global query filter can scope messages directly.</summary>
    public Guid BusinessId { get; set; }

    public MessageDirection Direction { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Conversation? Conversation { get; set; }
}
