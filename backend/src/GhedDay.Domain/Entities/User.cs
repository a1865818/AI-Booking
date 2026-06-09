using GhedDay.Domain.Enums;

namespace GhedDay.Domain.Entities;

/// <summary>
/// Owner / admin of a business. Super-admins have <see cref="BusinessId"/> = null.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Guid? BusinessId { get; set; }
    public UserRole Role { get; set; } = UserRole.Owner;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
