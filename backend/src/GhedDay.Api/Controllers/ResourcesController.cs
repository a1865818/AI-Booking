using GhedDay.Application.DTOs;
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

    public ResourcesController(GhedDayDbContext db) => _db = db;

    /// <summary>Resources for the current tenant (scoped by the global query filter).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResourceDto>>> GetAll(CancellationToken ct)
    {
        var resources = await _db.Resources
            .OrderBy(r => r.SortOrder)
            .Select(r => new ResourceDto(r.Id, r.Name, r.ResourceType, r.Capacity, r.IsActive, r.SortOrder))
            .ToListAsync(ct);

        return Ok(resources);
    }
}
