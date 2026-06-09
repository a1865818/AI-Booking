using GhedDay.Application.Common;

namespace GhedDay.Infrastructure.Data.QueryFilters;

/// <summary>
/// The single, auditable place where the tenant query filter is bypassed for super-admin
/// cross-tenant reads (non-negotiable rule 3). Toggles <see cref="GhedDayDbContext.IgnoreTenantFilter"/>
/// for the duration of the supplied work and always restores it.
/// </summary>
public sealed class QueryFilterDisabler : IQueryFilterDisabler
{
    private readonly GhedDayDbContext _db;

    public QueryFilterDisabler(GhedDayDbContext db) => _db = db;

    public async Task<T> RunWithoutTenantFilterAsync<T>(Func<Task<T>> work, CancellationToken ct = default)
    {
        var previous = _db.IgnoreTenantFilter;
        _db.IgnoreTenantFilter = true;
        try
        {
            return await work();
        }
        finally
        {
            _db.IgnoreTenantFilter = previous;
        }
    }
}
