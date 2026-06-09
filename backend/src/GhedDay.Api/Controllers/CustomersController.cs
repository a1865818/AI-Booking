using GhedDay.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CustomersController : ControllerBase
{
    private readonly GhedDayDbContext _db;

    public CustomersController(GhedDayDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var customers = await _db.Customers
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, c.Name, c.PhoneE164, c.LanguagePref, c.OptedOut })
            .ToListAsync(ct);

        return Ok(customers);
    }
}
