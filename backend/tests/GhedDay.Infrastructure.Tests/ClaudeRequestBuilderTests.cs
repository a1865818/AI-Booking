using GhedDay.Application.Verticals;
using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Domain.ValueObjects;
using GhedDay.Infrastructure.AI;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.Configuration;
using GhedDay.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace GhedDay.Infrastructure.Tests;

public class ClaudeRequestBuilderTests
{
    private readonly ClaudeRequestBuilder _sut = new(
        new VerticalConfigService(),
        Options.Create(new AnthropicOptions { Model = "claude-sonnet-4-6" }),
        new FixedTimeProvider(new DateTimeOffset(2030, 1, 1, 9, 0, 0, TimeSpan.Zero)));

    private static Business NailSalon()
    {
        var b = new Business { Name = "Lotus Nails", BusinessType = BusinessType.NailSalon, Timezone = "UTC" };
        b.SetVerticalConfig(new VerticalConfig { ResourceLabel = "Chair", ResourceLabelPlural = "Chairs", DepositRequired = true });
        return b;
    }

    private static Business Restaurant()
    {
        var b = new Business { Name = "Phở Sài Gòn", BusinessType = BusinessType.Restaurant, Timezone = "UTC" };
        b.SetVerticalConfig(new VerticalConfig
        {
            ResourceLabel = "Table",
            ResourceLabelPlural = "Tables",
            DepositThresholdPartySize = 6,
            DefaultDurationMinutes = 90,
        });
        return b;
    }

    [Fact]
    public void Restaurant_prompt_asks_for_party_size()
    {
        var prompt = _sut.BuildSystemPrompt(Restaurant());
        Assert.Contains("party size", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("table", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NailSalon_prompt_mentions_deposit_and_chair()
    {
        var prompt = _sut.BuildSystemPrompt(NailSalon());
        Assert.Contains("deposit", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("chair", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Prompt_includes_bilingual_and_current_date_guidance()
    {
        var prompt = _sut.BuildSystemPrompt(Restaurant());
        Assert.Contains("Vietnamese", prompt);
        Assert.Contains("2030", prompt); // current local date injected
    }

    [Fact]
    public void Build_carries_tools_and_messages_and_model()
    {
        var tools = new List<ToolDefinition> { new() { Name = "get_offerings" } };
        var messages = new List<ClaudeMessage> { ClaudeMessage.User("hi") };

        var request = _sut.Build(Restaurant(), messages, tools);

        Assert.Equal("claude-sonnet-4-6", request.Model);
        Assert.Single(request.Tools);
        Assert.Single(request.Messages);
        Assert.False(string.IsNullOrWhiteSpace(request.System));
    }
}
