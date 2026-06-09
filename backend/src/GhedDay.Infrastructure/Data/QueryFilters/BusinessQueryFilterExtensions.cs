using System.Linq.Expressions;
using GhedDay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Infrastructure.Data.QueryFilters;

/// <summary>
/// Applies the global <c>BusinessId</c> query filter to every tenant-scoped entity.
///
/// Each filter is <c>ignore() || entity.BusinessId == currentBusinessId()</c>. The two
/// accessor delegates are funcletised (evaluated as parameters) on every query, so toggling
/// the disabler or switching tenants between requests works without rebuilding the model.
/// </summary>
public static class BusinessQueryFilterExtensions
{
    public static void ApplyBusinessQueryFilters(
        this ModelBuilder modelBuilder,
        Func<Guid?> currentBusinessId,
        Func<bool> ignoreFilter)
    {
        // Business is keyed on its own Id rather than a BusinessId column.
        modelBuilder.Entity<Business>().HasQueryFilter(
            BuildFilter<Business>(nameof(Business.Id), currentBusinessId, ignoreFilter));

        ApplyOnBusinessId<Resource>(modelBuilder, currentBusinessId, ignoreFilter);
        ApplyOnBusinessId<Offering>(modelBuilder, currentBusinessId, ignoreFilter);
        ApplyOnBusinessId<BusinessHours>(modelBuilder, currentBusinessId, ignoreFilter);
        ApplyOnBusinessId<Customer>(modelBuilder, currentBusinessId, ignoreFilter);
        ApplyOnBusinessId<Conversation>(modelBuilder, currentBusinessId, ignoreFilter);
        ApplyOnBusinessId<Message>(modelBuilder, currentBusinessId, ignoreFilter);
        ApplyOnBusinessId<Booking>(modelBuilder, currentBusinessId, ignoreFilter);
        ApplyOnBusinessId<WaitlistEntry>(modelBuilder, currentBusinessId, ignoreFilter);
        ApplyOnBusinessId<Reminder>(modelBuilder, currentBusinessId, ignoreFilter);

        // User.BusinessId is nullable (super-admins have none) and ProcessedEvent is
        // intentionally cross-tenant; neither receives a tenant filter.
    }

    private static void ApplyOnBusinessId<TEntity>(
        ModelBuilder modelBuilder,
        Func<Guid?> currentBusinessId,
        Func<bool> ignoreFilter)
        where TEntity : class
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(
            BuildFilter<TEntity>("BusinessId", currentBusinessId, ignoreFilter));
    }

    private static Expression<Func<TEntity, bool>> BuildFilter<TEntity>(
        string propertyName,
        Func<Guid?> currentBusinessId,
        Func<bool> ignoreFilter)
    {
        var entity = Expression.Parameter(typeof(TEntity), "e");
        var property = Expression.Property(entity, propertyName);

        // entity.BusinessId == currentBusinessId() — compared as Guid? so a null tenant
        // (super-admin without the disabler) matches nothing.
        var businessIdCall = Expression.Convert(
            Expression.Invoke(Expression.Constant(currentBusinessId)),
            typeof(Guid?));
        var propertyAsNullable = Expression.Convert(property, typeof(Guid?));
        var equals = Expression.Equal(propertyAsNullable, businessIdCall);

        var ignoreCall = Expression.Invoke(Expression.Constant(ignoreFilter));
        var body = Expression.OrElse(ignoreCall, equals);

        return Expression.Lambda<Func<TEntity, bool>>(body, entity);
    }
}
