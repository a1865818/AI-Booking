using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhedDay.Domain.ValueObjects;

/// <summary>
/// Typed wrapper over the <c>businesses.vertical_config</c> jsonb column.
///
/// Adding a new vertical must never require a code change: extend this config and
/// <c>IVerticalConfigService</c> rather than branching on <c>BusinessType</c>
/// (non-negotiable rule 5).
/// </summary>
public sealed record VerticalConfig
{
    /// <summary>Singular resource label, e.g. "Chair", "Table", "Bay".</summary>
    [JsonPropertyName("resource_label")]
    public string ResourceLabel { get; init; } = "Resource";

    /// <summary>Plural resource label, e.g. "Chairs", "Tables", "Bays".</summary>
    [JsonPropertyName("resource_label_plural")]
    public string ResourceLabelPlural { get; init; } = "Resources";

    /// <summary>Whether a deposit is always required for this vertical.</summary>
    [JsonPropertyName("deposit_required")]
    public bool DepositRequired { get; init; }

    /// <summary>Minutes a booking hold stays alive before sweeping.</summary>
    [JsonPropertyName("hold_minutes")]
    public int HoldMinutes { get; init; } = 15;

    /// <summary>
    /// Restaurants only: party size at or above which a deposit becomes required even
    /// when <see cref="DepositRequired"/> is false.
    /// </summary>
    [JsonPropertyName("deposit_threshold_party_size")]
    public int? DepositThresholdPartySize { get; init; }

    /// <summary>Restaurants only: deposit charged per head, in cents.</summary>
    [JsonPropertyName("deposit_per_head_cents")]
    public int? DepositPerHeadCents { get; init; }

    /// <summary>Default booking duration when an offering does not specify one.</summary>
    [JsonPropertyName("default_duration_minutes")]
    public int? DefaultDurationMinutes { get; init; }

    [JsonPropertyName("party_size_min")]
    public int? PartySizeMin { get; init; }

    [JsonPropertyName("party_size_max")]
    public int? PartySizeMax { get; init; }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static VerticalConfig FromJson(string json) =>
        string.IsNullOrWhiteSpace(json)
            ? new VerticalConfig()
            : JsonSerializer.Deserialize<VerticalConfig>(json, SerializerOptions) ?? new VerticalConfig();

    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);
}
