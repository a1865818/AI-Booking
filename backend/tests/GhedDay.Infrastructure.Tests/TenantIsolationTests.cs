using GhedDay.Application.Common;
using GhedDay.Infrastructure.Data;
using GhedDay.Infrastructure.Data.QueryFilters;
using GhedDay.Infrastructure.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Infrastructure.Tests;

/// <summary>
/// Proves the global BusinessId query filter (non-negotiable rule 3). This is the regression
/// guard for the EF model-caching pitfall where a closure-captured tenant leaks across
/// contexts: each context must filter by its OWN tenant, re-evaluated per query.
/// </summary>
[Collection("postgres")]
public sealed class TenantIsolationTests
{
    private readonly PostgresFixture _pg;

    public TenantIsolationTests(PostgresFixture pg) => _pg = pg;

    private GhedDayDbContext NewContext(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<GhedDayDbContext>().UseNpgsql(_pg.ConnectionString).Options, tenant);

    [SkippableFact]
    public async Task Context_only_sees_its_own_tenant_resources()
    {
        Skip.IfNot(_pg.Available, "No Postgres available.");
        var a = await _pg.SeedBusinessAsync(2, 4);
        var b = await _pg.SeedBusinessAsync(8);
        try
        {
            await using var contextA = NewContext(new TenantStub(a.BusinessId));
            await using var contextB = NewContext(new TenantStub(b.BusinessId));

            var visibleToA = await contextA.Resources.ToListAsync();
            var visibleToB = await contextB.Resources.ToListAsync();

            Assert.All(visibleToA, r => Assert.Equal(a.BusinessId, r.BusinessId));
            Assert.All(visibleToB, r => Assert.Equal(b.BusinessId, r.BusinessId));
            Assert.DoesNotContain(visibleToA, r => r.BusinessId == b.BusinessId);
        }
        finally
        {
            await _pg.CleanupAsync(a.BusinessId);
            await _pg.CleanupAsync(b.BusinessId);
        }
    }

    [SkippableFact]
    public async Task Tenant_filter_is_per_context_not_captured_from_the_first()
    {
        Skip.IfNot(_pg.Available, "No Postgres available.");
        var a = await _pg.SeedBusinessAsync(2);
        var b = await _pg.SeedBusinessAsync(2);
        try
        {
            // Build a context for A first (this triggers model creation), then one for B.
            await using var contextA = NewContext(new TenantStub(a.BusinessId));
            _ = await contextA.Businesses.ToListAsync();

            await using var contextB = NewContext(new TenantStub(b.BusinessId));
            var businessesForB = await contextB.Businesses.Select(x => x.Id).ToListAsync();

            Assert.Contains(b.BusinessId, businessesForB);
            Assert.DoesNotContain(a.BusinessId, businessesForB);
        }
        finally
        {
            await _pg.CleanupAsync(a.BusinessId);
            await _pg.CleanupAsync(b.BusinessId);
        }
    }

    [SkippableFact]
    public async Task Query_filter_disabler_enables_cross_tenant_reads()
    {
        Skip.IfNot(_pg.Available, "No Postgres available.");
        var a = await _pg.SeedBusinessAsync(2);
        var b = await _pg.SeedBusinessAsync(2);
        try
        {
            await using var context = NewContext(new TenantStub(a.BusinessId));
            var disabler = new QueryFilterDisabler(context);

            var all = await disabler.RunWithoutTenantFilterAsync(() =>
                context.Businesses.Select(x => x.Id).ToListAsync());

            Assert.Contains(a.BusinessId, all);
            Assert.Contains(b.BusinessId, all);

            // Filter restored afterwards.
            var scoped = await context.Businesses.Select(x => x.Id).ToListAsync();
            Assert.DoesNotContain(b.BusinessId, scoped);
        }
        finally
        {
            await _pg.CleanupAsync(a.BusinessId);
            await _pg.CleanupAsync(b.BusinessId);
        }
    }
}
