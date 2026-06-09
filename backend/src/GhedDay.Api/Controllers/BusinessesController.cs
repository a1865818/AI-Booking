using GhedDay.Application.Common;
using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Domain.ValueObjects;
using GhedDay.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Api.Controllers;

/// <summary>
/// Super-admin only: provision new businesses. Cross-tenant reads go through the explicit
/// <see cref="IQueryFilterDisabler"/> wrapper (non-negotiable rule 3).
/// </summary>
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/[controller]")]
public sealed class BusinessesController : ControllerBase
{
    private readonly GhedDayDbContext _db;
    private readonly IQueryFilterDisabler _filterDisabler;

    public BusinessesController(GhedDayDbContext db, IQueryFilterDisabler filterDisabler)
    {
        _db = db;
        _filterDisabler = filterDisabler;
    }

    public sealed record ProvisionRequest(
        string Name,
        string Slug,
        string Timezone,
        BusinessType BusinessType,
        VerticalConfig VerticalConfig);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var businesses = await _filterDisabler.RunWithoutTenantFilterAsync(
            () => _db.Businesses
                .OrderBy(b => b.Name)
                .Select(b => new { b.Id, b.Name, b.Slug, BusinessType = b.BusinessType.ToString() })
                .ToListAsync(ct),
            ct);

        return Ok(businesses);
    }

    [HttpPost]
    public async Task<IActionResult> Provision([FromBody] ProvisionRequest request, CancellationToken ct)
    {
        var business = new Business
        {
            Name = request.Name,
            Slug = request.Slug,
            Timezone = request.Timezone,
            BusinessType = request.BusinessType
        };
        business.SetVerticalConfig(request.VerticalConfig);

        _db.Businesses.Add(business);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), new { id = business.Id }, new { business.Id });
    }
}
