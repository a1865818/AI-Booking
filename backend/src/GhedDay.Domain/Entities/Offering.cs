namespace GhedDay.Domain.Entities;

/// <summary>
/// A service for salons; "Table Reservation" + specials for restaurants. Was <c>Service</c>.
/// </summary>
public class Offering
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameVi { get; set; }
    public int DurationMinutes { get; set; }
    public int PriceCents { get; set; }

    /// <summary>True for "Table Reservation" — books a resource with no specific service.</summary>
    public bool IsResourceOnly { get; set; }

    public bool IsActive { get; set; } = true;

    public Business? Business { get; set; }
}
