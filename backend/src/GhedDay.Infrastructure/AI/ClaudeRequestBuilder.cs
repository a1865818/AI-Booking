using System.Text;
using GhedDay.Application.Verticals;
using GhedDay.Domain.Entities;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.AI.Tools;
using GhedDay.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GhedDay.Infrastructure.AI;

/// <summary>
/// Builds the Anthropic request for one turn: a vertical-aware system prompt (persona +
/// language + booking rules + the current local date) and the tools array. The same tools
/// serve every vertical; behaviour is driven by <c>vertical_config</c>, not by branching here.
/// </summary>
public sealed class ClaudeRequestBuilder
{
    private readonly IVerticalConfigService _verticalConfig;
    private readonly AnthropicOptions _options;
    private readonly TimeProvider _clock;

    public ClaudeRequestBuilder(
        IVerticalConfigService verticalConfig,
        IOptions<AnthropicOptions> options,
        TimeProvider clock)
    {
        _verticalConfig = verticalConfig;
        _options = options.Value;
        _clock = clock;
    }

    public ClaudeRequest Build(
        Business business,
        IReadOnlyList<ClaudeMessage> messages,
        IReadOnlyList<ToolDefinition> tools)
    {
        return new ClaudeRequest
        {
            Model = _options.Model,
            MaxTokens = 1024,
            System = BuildSystemPrompt(business),
            Tools = tools.ToList(),
            Messages = messages.ToList(),
        };
    }

    public string BuildSystemPrompt(Business business)
    {
        var config = _verticalConfig.GetConfig(business);
        var tz = CheckAvailabilityTool.ResolveTimeZone(business.Timezone);
        var localNow = TimeZoneInfo.ConvertTime(_clock.GetUtcNow(), tz);

        var sb = new StringBuilder();
        sb.Append($"You are the SMS booking assistant for {business.Name}. ");
        sb.Append(_verticalConfig.GetClaudePersonaHint(business)).Append(' ');

        sb.Append("Reply in the same language the customer writes in — English or Vietnamese. ");
        sb.Append("Keep replies short and friendly, suitable for SMS (1-3 sentences, no markdown). ");

        sb.Append($"The current local date and time is {localNow:dddd, dd MMM yyyy HH:mm} ({business.Timezone}); ");
        sb.Append("interpret relative dates like \"today\", \"tomorrow\", or weekday names accordingly. ");

        sb.Append("Booking rules: call check_availability before proposing any times, and never invent availability. ");
        sb.Append("Use get_offerings if you are unsure which services exist. ");
        sb.Append("Use create_booking_hold only after the customer confirms a specific time. ");
        sb.Append($"A booking reserves a {config.ResourceLabel.ToLowerInvariant()}. ");

        if (config.DepositThresholdPartySize is { } threshold)
            sb.Append($"Parties of {threshold} or more require a deposit; ask for the party size first. ");
        else if (config.DepositRequired)
            sb.Append("A deposit is required to confirm; let the customer know a payment link will follow. ");

        sb.Append("Never ask for or accept internal IDs from the customer. ");
        sb.Append("If you cannot help, offer to have a staff member follow up.");

        return sb.ToString();
    }
}
