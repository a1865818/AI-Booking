namespace GhedDay.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public string PhoneE164 { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string LanguagePref { get; set; } = "en";
    public bool OptedOut { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Business? Business { get; set; }
}
