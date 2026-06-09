using GhedDay.Application.DTOs;
using GhedDay.Application.Services;
using GhedDay.Application.Common;
using GhedDay.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class BookingsController : ControllerBase
{
    private readonly GhedDayDbContext _db;
    private readonly IAvailabilityService _availability;
    private readonly ITenantContext _tenant;

    public BookingsController(GhedDayDbContext db, IAvailabilityService availability, ITenantContext tenant)
    {
        _db = db;
        _availability = availability;
        _tenant = tenant;
    }

    /// <summary>Bookings for the current tenant on a given date (defaults to today, tenant-scoped).</summary>
    [HttpGet]
    public async Task<IActionResult> GetByDate([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var day = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dayStart = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var bookings = await _db.Bookings
            .Where(b => b.StartTime >= dayStart && b.StartTime < dayEnd)
            .OrderBy(b => b.StartTime)
            .Select(b => new
            {
                id = b.Id,
                customerName = b.Customer!.Name ?? b.Customer.PhoneE164,
                offeringName = b.Offering != null ? b.Offering.Name : null,
                resourceId = b.ResourceId,
                resourceName = b.Resource != null ? b.Resource.Name : null,
                startTime = b.StartTime,
                endTime = b.EndTime,
                partySize = b.PartySize,
                status = b.Status.ToString(),
            })
            .ToListAsync(ct);

        return Ok(bookings);
    }

    /// <summary>Available resource slots for a window (party_size optional, restaurants only).</summary>
    [HttpGet("availability")]
    public async Task<ActionResult<IReadOnlyList<SlotDto>>> GetAvailability(
        [FromQuery] DateTimeOffset start,
        [FromQuery] DateTimeOffset end,
        [FromQuery] int partySize,
        CancellationToken ct)
    {
        var requiredCapacity = partySize > 0 ? partySize : 1;
        var slots = await _availability.GetAvailableSlotsAsync(_tenant.RequireBusinessId(), start, end, requiredCapacity, ct);
        return Ok(slots);
    }
}
