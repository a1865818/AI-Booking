using System.Security.Claims;
using GhedDay.Application.Common;

namespace GhedDay.Api.Middleware;

/// <summary>
/// Per-request tenant, resolved from the authenticated principal's claims by
/// <see cref="TenantResolutionMiddleware"/>. Tenant identifiers come from here only —
/// never from a request body (non-negotiable rules 1 &amp; 3).
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid? BusinessId { get; private set; }
    public bool IsSuperAdmin { get; private set; }

    public void Set(Guid? businessId, bool isSuperAdmin)
    {
        BusinessId = businessId;
        IsSuperAdmin = isSuperAdmin;
    }

    public Guid RequireBusinessId() =>
        BusinessId ?? throw new InvalidOperationException("No tenant resolved for the current request.");
}

public sealed class TenantResolutionMiddleware
{
    public const string BusinessIdClaim = "business_id";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (tenantContext is TenantContext concrete && context.User.Identity?.IsAuthenticated == true)
        {
            var isSuperAdmin = context.User.IsInRole("SuperAdmin");
            var businessIdValue = context.User.FindFirstValue(BusinessIdClaim);

            Guid? businessId = Guid.TryParse(businessIdValue, out var parsed) ? parsed : null;
            concrete.Set(businessId, isSuperAdmin);
        }

        await _next(context);
    }
}
