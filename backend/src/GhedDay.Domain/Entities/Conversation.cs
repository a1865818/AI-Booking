using GhedDay.Domain.Enums;

namespace GhedDay.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    public bool AiEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Customer? Customer { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
