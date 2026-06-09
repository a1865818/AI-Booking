namespace GhedDay.Domain.Enums;

/// <summary>
/// Vertical the business operates in. Behaviour is driven by <c>vertical_config</c>,
/// never by branching on this enum — see non-negotiable rule 5.
/// </summary>
public enum BusinessType
{
    NailSalon,
    Restaurant,
    Barbershop,
    Spa,
    Beauty,
    Other
}
