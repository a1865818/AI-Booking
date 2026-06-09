using System.Text.Json;
using GhedDay.Domain.Entities;
using GhedDay.Infrastructure.AI.Models;

namespace GhedDay.Infrastructure.AI.Tools;

/// <summary>
/// The tenant + customer the tool runs for. Both ids originate from the resolved server-side
/// context (Twilio number → business, From number → customer) — never from Claude's tool
/// arguments (non-negotiable rule 1).
/// </summary>
public sealed record ToolContext(Business Business, Customer Customer)
{
    public Guid BusinessId => Business.Id;
    public Guid CustomerId => Customer.Id;
}

/// <summary>A single Claude tool: its name, its (vertical-aware) definition, and its handler.</summary>
public interface IClaudeTool
{
    string Name { get; }

    ToolDefinition GetDefinition(Business business);

    /// <summary>Executes the tool and returns the JSON string sent back as the tool_result.</summary>
    Task<string> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct = default);
}
