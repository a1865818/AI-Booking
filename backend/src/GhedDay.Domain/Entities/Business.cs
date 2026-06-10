using GhedDay.Domain.Enums;
using GhedDay.Domain.ValueObjects;

namespace GhedDay.Domain.Entities;

/// <summary>
/// A tenant. Every business is one row regardless of vertical. Was <c>Salon</c>.
/// </summary>
public class Business
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? OwnerUserId { get; set; }
    public string? TwilioNumber { get; set; }
    public string? StripeAccountId { get; set; }
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public BusinessType BusinessType { get; set; } = BusinessType.Other;

    /// <summary>Raw vertical-specific config (jsonb). Use <see cref="GetVerticalConfig"/> for the typed view.</summary>
    public string VerticalConfigJson { get; set; } = "{}";

    /// <summary>Persona, deposit policy, hold_minutes, etc. (jsonb).</summary>
    public string? SettingsJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    public ICollection<Offering> Offerings { get; set; } = new List<Offering>();
    public ICollection<BusinessHours> Hours { get; set; } = new List<BusinessHours>();

    public VerticalConfig GetVerticalConfig() => VerticalConfig.FromJson(VerticalConfigJson);
    public void SetVerticalConfig(VerticalConfig config) => VerticalConfigJson = config.ToJson();

    public BusinessSettings GetSettings() => BusinessSettings.FromJson(SettingsJson);
    public void SetSettings(BusinessSettings settings) => SettingsJson = settings.ToJson();
}
