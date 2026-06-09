using GhedDay.Domain.Enums;

namespace GhedDay.Domain.Entities;

/// <summary>
/// A bookable unit — technician, table, chair, room, or bay. Was <c>Staff</c>.
/// </summary>
public class Resource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ResourceType ResourceType { get; set; } = ResourceType.Technician;

    /// <summary>1 for staff/chairs; N for restaurant tables.</summary>
    public int Capacity { get; set; } = 1;

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public Business? Business { get; set; }
}
