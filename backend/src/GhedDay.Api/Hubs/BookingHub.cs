using System.Security.Claims;
using GhedDay.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GhedDay.Api.Hubs;

/// <summary>
/// JWT-authenticated real-time hub. Each connection joins the group
/// <c>business_{businessId}</c> derived from its own claim, so events never leak across
/// tenants (PLAN §2.7).
/// </summary>
[Authorize]
public sealed class BookingHub : Hub
{
    public static string GroupName(Guid businessId) => $"business_{businessId}";

    public override async Task OnConnectedAsync()
    {
        var businessIdValue = Context.User?.FindFirstValue(TenantResolutionMiddleware.BusinessIdClaim);
        if (Guid.TryParse(businessIdValue, out var businessId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(businessId));
        }

        await base.OnConnectedAsync();
    }
}
