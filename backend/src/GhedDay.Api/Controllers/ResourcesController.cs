using GhedDay.Application.Common;
using GhedDay.Application.DTOs;
using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ResourcesController : ControllerBase
{
    private readonly GhedDayDbContext _db;
    private readonly ITenantContext _tenant;

    public ResourcesController(GhedDayDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResourceDto>>> GetAll(CancellationToken ct)
    {
        var resources = await _db.Resources
            .OrderBy(r => r.SortOrder)
            .Select(r => new ResourceDto(r.Id, r.Name, r.ResourceType, r.Capacity, r.IsActive, r.SortOrder))
            .ToListAsync(ct);

        return Ok(resources);
    }

    [HttpPost]
    public async Task<ActionResult<ResourceDto>> Create([FromBody] CreateResourceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        var businessId = _tenant.RequireBusinessId();
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, ct);
        if (business is null)
            return NotFound();

        var maxSort = await _db.Resources.MaxAsync(r => (int?)r.SortOrder, ct) ?? 0;
        var resource = new Resource
        {
            BusinessId = businessId,
            Name = request.Name.Trim(),
            Capacity = request.Capacity is > 0 ? request.Capacity.Value : 1,
            ResourceType = InferResourceType(business.BusinessType),
            SortOrder = maxSort + 1,
            IsActive = true,
        };

        _db.Resources.Add(resource);
        await _db.SaveChangesAsync(ct);

        return Ok(new ResourceDto(
            resource.Id, resource.Name, resource.ResourceType, resource.Capacity, resource.IsActive, resource.SortOrder));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ResourceDto>> Update(Guid id, [FromBody] UpdateResourceRequest request, CancellationToken ct)
    {
        var resource = await _db.Resources.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (resource is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name))
            resource.Name = request.Name.Trim();

        if (request.Capacity is > 0)
            resource.Capacity = request.Capacity.Value;

        if (request.IsActive.HasValue)
            resource.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync(ct);

        return Ok(new ResourceDto(
            resource.Id, resource.Name, resource.ResourceType, resource.Capacity, resource.IsActive, resource.SortOrder));
    }

    public sealed record UpdateResourceRequest(string? Name, int? Capacity, bool? IsActive);

    public sealed record CreateResourceRequest(string Name, int? Capacity);

    private static ResourceType InferResourceType(BusinessType type) => type switch
    {
        BusinessType.Restaurant => ResourceType.Table,
        BusinessType.NailSalon => ResourceType.Chair,
        BusinessType.Barbershop => ResourceType.Chair,
        BusinessType.Spa => ResourceType.Room,
        BusinessType.Beauty => ResourceType.Technician,
        _ => ResourceType.Technician,
    };
}
