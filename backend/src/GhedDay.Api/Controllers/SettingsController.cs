using GhedDay.Application.Common;
using GhedDay.Application.Verticals;
using GhedDay.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class SettingsController : ControllerBase
{
    private readonly GhedDayDbContext _db;
    private readonly IVerticalConfigService _verticalConfig;
    private readonly ITenantContext _tenant;

    public SettingsController(GhedDayDbContext db, IVerticalConfigService verticalConfig, ITenantContext tenant)
    {
        _db = db;
        _verticalConfig = verticalConfig;
        _tenant = tenant;
    }

    /// <summary>Current tenant's settings incl. resolved vertical labels used by the dashboard UI.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == _tenant.RequireBusinessId(), ct);
        if (business is null)
            return NotFound();

        return Ok(new
        {
            business.Id,
            business.Name,
            business.Timezone,
            BusinessType = business.BusinessType.ToString(),
            ResourceLabel = _verticalConfig.GetResourceLabel(business),
            ResourceLabelPlural = _verticalConfig.GetResourceLabel(business, plural: true),
            VerticalConfig = business.GetVerticalConfig()
        });
    }
}
