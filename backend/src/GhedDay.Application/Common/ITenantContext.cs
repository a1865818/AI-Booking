namespace GhedDay.Application.Common;

/// <summary>
/// The current tenant, resolved from the JWT <c>sub</c> claim by TenantResolutionMiddleware
/// (or bound server-side on the Twilio webhook from the receiving number).
///
/// Non-negotiable rule 1 &amp; 3: tenant identifiers are read from here only — never from a
/// request body or a Claude tool argument.
/// </summary>
public interface ITenantContext
{
    /// <summary>The resolved business id, or null for unauthenticated / super-admin contexts.</summary>
    Guid? BusinessId { get; }

    bool IsSuperAdmin { get; }

    /// <summary>The business id, throwing if no tenant has been resolved.</summary>
    Guid RequireBusinessId();
}
