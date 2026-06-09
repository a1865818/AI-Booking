using GhedDay.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GhedDay.Infrastructure.Data;

/// <summary>
/// Lets <c>dotnet ef</c> build the context at design time. Uses a no-tenant context — design
/// time never executes tenant-scoped queries.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GhedDayDbContext>
{
    public GhedDayDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("GHEDDAY_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=ghedday;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<GhedDayDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new GhedDayDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? BusinessId => null;
        public bool IsSuperAdmin => true;
        public Guid RequireBusinessId() => throw new InvalidOperationException("No tenant at design time.");
    }
}
