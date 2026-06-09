using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhedDay.Infrastructure.AI.Models;

/// <summary>Shared serializer settings for the Anthropic Messages API wire format.</summary>
public static class ClaudeJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };
}

/// <summary>One content block — text, tool_use (from Claude), or tool_result (back to Claude).</summary>
public sealed class ContentBlock
{
    [JsonPropertyName("type")] public string Type { get; set; } = "text";
    [JsonPropertyName("text")] public string? Text { get; set; }

    // tool_use (assistant → us)
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("input")] public JsonElement? Input { get; set; }

    // tool_result (us → assistant)
    [JsonPropertyName("tool_use_id")] public string? ToolUseId { get; set; }
    [JsonPropertyName("content")] public string? Content { get; set; }

    public static ContentBlock TextBlock(string text) => new() { Type = "text", Text = text };

    public static ContentBlock ToolResult(string toolUseId, string content) =>
        new() { Type = "tool_result", ToolUseId = toolUseId, Content = content };
}

public sealed class ClaudeMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = "user";
    [JsonPropertyName("content")] public List<ContentBlock> Content { get; set; } = [];

    public static ClaudeMessage User(string text) =>
        new() { Role = "user", Content = [ContentBlock.TextBlock(text)] };

    public static ClaudeMessage Assistant(string text) =>
        new() { Role = "assistant", Content = [ContentBlock.TextBlock(text)] };
}

public sealed class ToolDefinition
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("input_schema")] public object InputSchema { get; set; } = new { };
}

public sealed class ClaudeRequest
{
    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
    [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; } = 1024;
    [JsonPropertyName("system")] public string? System { get; set; }
    [JsonPropertyName("tools")] public List<ToolDefinition> Tools { get; set; } = [];
    [JsonPropertyName("messages")] public List<ClaudeMessage> Messages { get; set; } = [];
}

public sealed class ClaudeResponse
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("role")] public string? Role { get; set; }
    [JsonPropertyName("model")] public string? Model { get; set; }
    [JsonPropertyName("stop_reason")] public string? StopReason { get; set; }
    [JsonPropertyName("content")] public List<ContentBlock> Content { get; set; } = [];

    public bool RequiresToolUse => StopReason == "tool_use";

    public IEnumerable<ContentBlock> ToolUses => Content.Where(c => c.Type == "tool_use");

    public string ConcatenatedText() =>
        string.Join(" ", Content.Where(c => c.Type == "text" && !string.IsNullOrWhiteSpace(c.Text)).Select(c => c.Text)).Trim();
}

/// <summary>
/// Typed Anthropic Messages client. Abstracted so the orchestrator loop can be unit-tested
/// with scripted responses and no network.
/// </summary>
public interface IClaudeClient
{
    Task<ClaudeResponse> CreateMessageAsync(ClaudeRequest request, CancellationToken ct = default);
}
