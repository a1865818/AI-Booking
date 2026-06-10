using GhedDay.Application.Common;
using GhedDay.Application.DTOs;
using GhedDay.Domain.Entities;
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
    private readonly ITenantContext _tenant;

    public OfferingsController(GhedDayDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OfferingDto>>> GetAll(CancellationToken ct)
    {
        var offerings = await _db.Offerings
            .OrderBy(o => o.Name)
            .Select(o => new OfferingDto(o.Id, o.Name, o.NameVi, o.DurationMinutes, o.PriceCents, o.IsResourceOnly, o.IsActive))
            .ToListAsync(ct);

        return Ok(offerings);
    }

    [HttpPost]
    public async Task<ActionResult<OfferingDto>> Create([FromBody] UpsertOfferingRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        var businessId = _tenant.RequireBusinessId();
        var offering = new Offering
        {
            BusinessId = businessId,
            Name = request.Name.Trim(),
            NameVi = request.NameVi?.Trim(),
            DurationMinutes = request.DurationMinutes > 0 ? request.DurationMinutes : 60,
            PriceCents = Math.Max(0, request.PriceCents),
            IsResourceOnly = request.IsResourceOnly,
            IsActive = true,
        };

        _db.Offerings.Add(offering);
        await _db.SaveChangesAsync(ct);

        return Ok(new OfferingDto(
            offering.Id, offering.Name, offering.NameVi, offering.DurationMinutes,
            offering.PriceCents, offering.IsResourceOnly, offering.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OfferingDto>> Update(Guid id, [FromBody] UpsertOfferingRequest request, CancellationToken ct)
    {
        var offering = await _db.Offerings.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (offering is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name))
            offering.Name = request.Name.Trim();

        offering.NameVi = request.NameVi?.Trim();
        if (request.DurationMinutes > 0)
            offering.DurationMinutes = request.DurationMinutes;

        offering.PriceCents = Math.Max(0, request.PriceCents);
        offering.IsResourceOnly = request.IsResourceOnly;
        if (request.IsActive.HasValue)
            offering.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync(ct);

        return Ok(new OfferingDto(
            offering.Id, offering.Name, offering.NameVi, offering.DurationMinutes,
            offering.PriceCents, offering.IsResourceOnly, offering.IsActive));
    }

    public sealed record UpsertOfferingRequest(
        string Name,
        string? NameVi,
        int DurationMinutes,
        int PriceCents,
        bool IsResourceOnly = false,
        bool? IsActive = null);
}
