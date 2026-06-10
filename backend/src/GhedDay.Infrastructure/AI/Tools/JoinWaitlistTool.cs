using System.Globalization;
using System.Text.Json;
using GhedDay.Application.Waitlist;
using GhedDay.Domain.Entities;
using GhedDay.Infrastructure.AI.Models;

namespace GhedDay.Infrastructure.AI.Tools;

/// <summary>Adds the customer to the waitlist when no slots are available.</summary>
public sealed class JoinWaitlistTool : IClaudeTool
{
    private readonly IWaitlistRepository _waitlist;

    public JoinWaitlistTool(IWaitlistRepository waitlist) => _waitlist = waitlist;

    public string Name => "join_waitlist";

    public ToolDefinition GetDefinition(Business business) => new()
    {
        Name = Name,
        Description =
            "Add the customer to the waitlist for a preferred date when no suitable slots are free. " +
            "They will receive an SMS if a slot opens.",
        InputSchema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["preferred_date"] = Prop("string", "Preferred date in YYYY-MM-DD (business local time)."),
                ["offering_id"] = Prop("string", "Optional offering id."),
                ["party_size"] = Prop("integer", "Number of guests (restaurants)."),
            },
            ["required"] = new[] { "preferred_date" },
        },
    };

    public async Task<string> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct = default)
    {
        var dateRaw = input.GetString("preferred_date");
        if (!DateOnly.TryParse(dateRaw, CultureInfo.InvariantCulture, out var preferredDate))
            return ToolJson.Serialize(new { success = false, reason = "Invalid or missing 'preferred_date'." });

        var partySize = input.GetInt("party_size");
        var offeringId = input.GetGuid("offering_id");

        var entryId = await _waitlist.AddEntryAsync(new AddWaitlistEntryRequest
        {
            BusinessId = context.BusinessId,
            CustomerId = context.CustomerId,
            OfferingId = offeringId,
            PreferredDate = preferredDate,
            PartySize = partySize,
        }, ct);

        return ToolJson.Serialize(new
        {
            success = true,
            waitlist_entry_id = entryId,
            preferred_date = preferredDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            message = "Customer added to waitlist; they will be texted if a slot opens.",
        });
    }

    private static Dictionary<string, object?> Prop(string type, string description) =>
        new() { ["type"] = type, ["description"] = description };
}
