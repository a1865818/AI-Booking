using System.Text.Json;

namespace GhedDay.Infrastructure.AI.Tools;

/// <summary>Small helpers for reading Claude tool inputs and emitting tool_result JSON.</summary>
internal static class ToolJson
{
    public static string Serialize(object value) => JsonSerializer.Serialize(value);

    public static string? GetString(this JsonElement input, string name) =>
        input.ValueKind == JsonValueKind.Object
        && input.TryGetProperty(name, out var prop)
        && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    public static int? GetInt(this JsonElement input, string name)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty(name, out var prop))
            return null;
        return prop.ValueKind switch
        {
            JsonValueKind.Number when prop.TryGetInt32(out var n) => n,
            JsonValueKind.String when int.TryParse(prop.GetString(), out var n) => n,
            _ => null,
        };
    }

    public static Guid? GetGuid(this JsonElement input, string name)
    {
        var raw = input.GetString(name);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
