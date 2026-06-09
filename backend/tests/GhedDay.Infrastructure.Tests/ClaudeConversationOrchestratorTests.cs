using GhedDay.Application.Verticals;
using GhedDay.Infrastructure.AI;
using GhedDay.Infrastructure.Configuration;
using GhedDay.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GhedDay.Infrastructure.Tests;

public class ClaudeConversationOrchestratorTests
{
    private static ClaudeRequestBuilder Builder() => new(
        new VerticalConfigService(),
        Options.Create(new AnthropicOptions()),
        new FixedTimeProvider(new DateTimeOffset(2030, 1, 1, 9, 0, 0, TimeSpan.Zero)));

    private static ClaudeConversationOrchestrator Build(
        ScriptedClaudeClient claude,
        IConversationContextStore store,
        RecordingToolHandler tools,
        CapturingSmsService sms,
        int maxIterations = 8) =>
        new(
            claude,
            Builder(),
            tools,
            store,
            sms,
            new NullNotificationService(),
            Options.Create(new AnthropicOptions { MaxIterations = maxIterations }),
            NullLogger<ClaudeConversationOrchestrator>.Instance);

    [Fact]
    public async Task End_turn_sends_and_persists_reply()
    {
        var store = new InMemoryConversationStore(InMemoryConversationStore.Context());
        var sms = new CapturingSmsService();
        var orchestrator = Build(new ScriptedClaudeClient(ScriptedClaudeClient.EndTurn("Hello!")), store, new RecordingToolHandler(), sms);

        await orchestrator.HandleInboundMessageAsync(Guid.NewGuid(), Guid.NewGuid(), "hi");

        Assert.Contains(sms.Sent, m => m.Body == "Hello!");
        Assert.Contains("Hello!", store.Outbound);
    }

    [Fact]
    public async Task Tool_use_round_then_end_turn_executes_tool()
    {
        var store = new InMemoryConversationStore(InMemoryConversationStore.Context());
        var sms = new CapturingSmsService();
        var tools = new RecordingToolHandler("{\"offerings\":[]}");
        var claude = new ScriptedClaudeClient(
            ScriptedClaudeClient.ToolUse("get_offerings", "t1", new { }),
            ScriptedClaudeClient.EndTurn("Here are our services"));

        var orchestrator = Build(claude, store, tools, sms);
        await orchestrator.HandleInboundMessageAsync(Guid.NewGuid(), Guid.NewGuid(), "what do you offer?");

        Assert.Contains("get_offerings", tools.Invocations);
        Assert.Contains(sms.Sent, m => m.Body == "Here are our services");
    }

    [Fact]
    public async Task Exceeding_iterations_sends_bilingual_fallback()
    {
        var store = new InMemoryConversationStore(InMemoryConversationStore.Context());
        var sms = new CapturingSmsService();
        var claude = new ScriptedClaudeClient(
            ScriptedClaudeClient.ToolUse("a", "1", new { }),
            ScriptedClaudeClient.ToolUse("b", "2", new { }));

        var orchestrator = Build(claude, store, new RecordingToolHandler(), sms, maxIterations: 2);
        await orchestrator.HandleInboundMessageAsync(Guid.NewGuid(), Guid.NewGuid(), "loop please");

        Assert.Contains(sms.Sent, m => m.Body.Contains("trouble") && m.Body.Contains("Xin lỗi"));
    }

    [Fact]
    public async Task Opted_out_customer_gets_no_reply()
    {
        var store = new InMemoryConversationStore(InMemoryConversationStore.Context(optedOut: true));
        var sms = new CapturingSmsService();
        var orchestrator = Build(new ScriptedClaudeClient(ScriptedClaudeClient.EndTurn("Hello!")), store, new RecordingToolHandler(), sms);

        await orchestrator.HandleInboundMessageAsync(Guid.NewGuid(), Guid.NewGuid(), "hi");

        Assert.Empty(sms.Sent);
        Assert.Empty(store.Outbound);
    }
}
