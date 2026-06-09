using GhedDay.Application.Common;
using GhedDay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Infrastructure.Data;

/// <summary>
/// EF Core context. Every tenant-scoped entity carries a global query filter keyed on the
/// current <see cref="ITenantContext"/> BusinessId (non-negotiable rule 3). Super-admin
/// cross-tenant reads must go through <see cref="QueryFilters.QueryFilterDisabler"/>.
///
/// The filters reference the context instance members <see cref="CurrentBusinessId"/> and
/// <see cref="IgnoreTenantFilter"/> so EF Core re-evaluates them against the executing context
/// on every query. Capturing the tenant in a closure would bind the value at model-build time
/// (the model is cached) and leak the first request's tenant into all later ones.
/// </summary>
public class GhedDayDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public GhedDayDbContext(DbContextOptions<GhedDayDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    /// <summary>Current tenant id, read per query by the global filters.</summary>
    public Guid? CurrentBusinessId => _tenant.BusinessId;

    /// <summary>
    /// When true the BusinessId global filter is bypassed. Toggled only by the explicit
    /// <see cref="QueryFilters.QueryFilterDisabler"/> wrapper — never set ad hoc.
    /// </summary>
    public bool IgnoreTenantFilter { get; set; }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Offering> Offerings => Set<Offering>();
    public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GhedDayDbContext).Assembly);

        // Business is keyed on its own Id; the rest carry a BusinessId column.
        modelBuilder.Entity<Business>().HasQueryFilter(e => IgnoreTenantFilter || e.Id == CurrentBusinessId);
        modelBuilder.Entity<Resource>().HasQueryFilter(e => IgnoreTenantFilter || e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Offering>().HasQueryFilter(e => IgnoreTenantFilter || e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<BusinessHours>().HasQueryFilter(e => IgnoreTenantFilter || e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Customer>().HasQueryFilter(e => IgnoreTenantFilter || e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Conversation>().HasQueryFilter(e => IgnoreTenantFilter || e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Message>().HasQueryFilter(e => IgnoreTenantFilter || e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Booking>().HasQueryFilter(e => IgnoreTenantFilter || e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<WaitlistEntry>().HasQueryFilter(e => IgnoreTenantFilter || e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Reminder>().HasQueryFilter(e => IgnoreTenantFilter || e.BusinessId == CurrentBusinessId);

        // User.BusinessId is nullable (super-admins have none) and ProcessedEvent is
        // intentionally cross-tenant; neither receives a tenant filter.
    }
}
