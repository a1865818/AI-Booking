using GhedDay.Application.Common;
using GhedDay.Domain.Entities;
using GhedDay.Infrastructure.Data.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Infrastructure.Data;

/// <summary>
/// EF Core context. Every tenant-scoped entity carries a global query filter keyed on the
/// current <see cref="ITenantContext"/> BusinessId (non-negotiable rule 3). Super-admin
/// cross-tenant reads must go through <see cref="QueryFilterDisabler"/>.
/// </summary>
public class GhedDayDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    /// <summary>
    /// When true the BusinessId global filter is bypassed. Toggled only by the explicit
    /// <see cref="QueryFilterDisabler"/> wrapper — never set ad hoc.
    /// </summary>
    internal bool IgnoreTenantFilter { get; set; }

    public GhedDayDbContext(DbContextOptions<GhedDayDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<User> Users => Set<User>();
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
        modelBuilder.ApplyBusinessQueryFilters(() => _tenant.BusinessId, () => IgnoreTenantFilter);
    }
}
