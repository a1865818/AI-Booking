using GhedDay.Application.DTOs;
using GhedDay.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class OfferingsController : ControllerBase
{
    private readonly GhedDayDbContext _db;

    public OfferingsController(GhedDayDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OfferingDto>>> GetAll(CancellationToken ct)
    {
        var offerings = await _db.Offerings
            .Where(o => o.IsActive)
            .Select(o => new OfferingDto(o.Id, o.Name, o.NameVi, o.DurationMinutes, o.PriceCents, o.IsResourceOnly, o.IsActive))
            .ToListAsync(ct);

        return Ok(offerings);
    }
}
