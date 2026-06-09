using System.Text.Json;
using GhedDay.Domain.Entities;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.AI.Tools;
using Microsoft.Extensions.Logging;

namespace GhedDay.Infrastructure.AI;

/// <summary>Resolves a <c>tool_use</c> name to its handler and returns the tool_result content.</summary>
public interface IClaudeToolHandler
{
    IReadOnlyList<ToolDefinition> GetDefinitions(Business business);

    Task<string> ExecuteAsync(string toolName, JsonElement input, ToolContext context, CancellationToken ct = default);
}

public sealed class ClaudeToolHandler : IClaudeToolHandler
{
    private readonly IReadOnlyDictionary<string, IClaudeTool> _tools;
    private readonly ILogger<ClaudeToolHandler> _logger;

    public ClaudeToolHandler(IEnumerable<IClaudeTool> tools, ILogger<ClaudeToolHandler> logger)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public IReadOnlyList<ToolDefinition> GetDefinitions(Business business) =>
        _tools.Values.Select(t => t.GetDefinition(business)).ToList();

    public async Task<string> ExecuteAsync(string toolName, JsonElement input, ToolContext context, CancellationToken ct = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            _logger.LogWarning("Claude requested unknown tool {Tool}", toolName);
            return JsonSerializer.Serialize(new { error = $"Unknown tool '{toolName}'." });
        }

        try
        {
            return await tool.ExecuteAsync(input, context, ct);
        }
        catch (Exception ex)
        {
            // Surface the failure to Claude as a tool_result rather than aborting the loop.
            _logger.LogError(ex, "Tool {Tool} failed for business {BusinessId}", toolName, context.BusinessId);
            return JsonSerializer.Serialize(new { error = "The tool failed to execute. Apologize and offer to try again." });
        }
    }
}
