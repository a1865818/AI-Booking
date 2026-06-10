using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhedDay.Domain.ValueObjects;

/// <summary>Typed wrapper over the <c>businesses.settings</c> jsonb column (persona, etc.).</summary>
public sealed record BusinessSettings
{
    /// <summary>Owner-defined tone/instructions appended to the Claude system prompt.</summary>
    [JsonPropertyName("ai_persona")]
    public string? AiPersona { get; init; }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static BusinessSettings FromJson(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? new BusinessSettings()
            : JsonSerializer.Deserialize<BusinessSettings>(json, SerializerOptions) ?? new BusinessSettings();

    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);
}
