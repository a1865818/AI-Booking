using System.Globalization;
using System.Text.Json;
using GhedDay.Domain.Entities;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Infrastructure.AI.Tools;

/// <summary>Returns the business's weekly opening hours.</summary>
public sealed class GetBusinessHoursTool : IClaudeTool
{
    private readonly GhedDayDbContext _db;

    public GetBusinessHoursTool(GhedDayDbContext db) => _db = db;

    public string Name => "get_business_hours";

    public ToolDefinition GetDefinition(Business business) => new()
    {
        Name = Name,
        Description = "Returns the business opening hours for each day of the week.",
        InputSchema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>(),
        },
    };

    public async Task<string> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct = default)
    {
        var hours = await _db.BusinessHours
            .OrderBy(h => h.DayOfWeek)
            .Select(h => new
            {
                day = DayName(h.DayOfWeek),
                day_of_week = h.DayOfWeek,
                open = h.OpenTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                close = h.CloseTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            })
            .ToListAsync(ct);

        return ToolJson.Serialize(new { timezone = context.Business.Timezone, hours });
    }

    private static string DayName(int dayOfWeek) => dayOfWeek switch
    {
        0 => "Sunday",
        1 => "Monday",
        2 => "Tuesday",
        3 => "Wednesday",
        4 => "Thursday",
        5 => "Friday",
        6 => "Saturday",
        _ => dayOfWeek.ToString(CultureInfo.InvariantCulture),
    };
}
