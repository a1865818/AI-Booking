namespace GhedDay.Domain.Enums;

/// <summary>
/// A bookable unit. One model fits all verticals: a technician, table, chair, room, or bay.
/// </summary>
public enum ResourceType
{
    Technician,
    Table,
    Chair,
    Room,
    Bay
}
