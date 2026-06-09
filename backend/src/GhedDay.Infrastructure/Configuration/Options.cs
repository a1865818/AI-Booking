namespace GhedDay.Infrastructure.Configuration;

public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-6";
    public string ApiVersion { get; set; } = "2023-06-01";
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public int MaxIterations { get; set; } = 8;
}

public sealed class TwilioOptions
{
    public const string SectionName = "Twilio";
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
}

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
