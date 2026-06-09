using System.Text.Json;
using GhedDay.Domain.Entities;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Infrastructure.AI.Tools;

/// <summary>Returns the current business's active offerings (services / reservation types).</summary>
public sealed class GetOfferingsTool : IClaudeTool
{
    private readonly GhedDayDbContext _db;

    public GetOfferingsTool(GhedDayDbContext db) => _db = db;

    public string Name => "get_offerings";

    public ToolDefinition GetDefinition(Business business) => new()
    {
        Name = Name,
        Description = "List the services / reservation types this business offers, with durations and prices.",
        InputSchema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>(),
        },
    };

    public async Task<string> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct = default)
    {
        var offerings = await _db.Offerings
            .Where(o => o.IsActive)
            .OrderBy(o => o.Name)
            .Select(o => new
            {
                offering_id = o.Id,
                name = o.Name,
                name_vi = o.NameVi,
                duration_minutes = o.DurationMinutes,
                price_cents = o.PriceCents,
                is_resource_only = o.IsResourceOnly,
            })
            .ToListAsync(ct);

        return ToolJson.Serialize(new { offerings });
    }
}
