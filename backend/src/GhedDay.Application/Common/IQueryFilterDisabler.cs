namespace GhedDay.Application.Common;

/// <summary>
/// Explicit, auditable escape hatch for super-admin cross-tenant reads. Any code path that
/// needs to bypass the global <c>BusinessId</c> query filter must go through this wrapper so
/// the bypass is greppable and reviewable (non-negotiable rule 3).
/// </summary>
public interface IQueryFilterDisabler
{
    /// <summary>Runs <paramref name="work"/> with the global BusinessId query filter disabled.</summary>
    Task<T> RunWithoutTenantFilterAsync<T>(Func<Task<T>> work, CancellationToken ct = default);
}
