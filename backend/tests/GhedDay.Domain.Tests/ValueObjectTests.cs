using GhedDay.Domain.ValueObjects;
using Xunit;

namespace GhedDay.Domain.Tests;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+14155552671", "+14155552671")]
    [InlineData("+1 (415) 555-2671", "+14155552671")]
    [InlineData("+84 90 123 4567", "+84901234567")]
    public void Parse_normalises_to_e164(string raw, string expected)
    {
        Assert.Equal(expected, PhoneNumber.Parse(raw).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-phone")]
    [InlineData("5551234")]      // no country code
    [InlineData("+0123456789")]  // leading zero after +
    public void TryParse_rejects_invalid_numbers(string raw)
    {
        Assert.False(PhoneNumber.TryParse(raw, out _));
    }

    [Fact]
    public void Equality_is_by_value()
    {
        Assert.Equal(PhoneNumber.Parse("+14155552671"), PhoneNumber.Parse("+1 415 555 2671"));
    }
}

public class TimeSlotTests
{
    private static DateTimeOffset At(int hour) => new(2026, 1, 1, hour, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Overlaps_is_true_for_intersecting_intervals()
    {
        var a = new TimeSlot(At(9), At(11));
        var b = new TimeSlot(At(10), At(12));
        Assert.True(a.Overlaps(b));
    }

    [Fact]
    public void Adjacent_intervals_do_not_overlap()
    {
        var a = new TimeSlot(At(9), At(10));
        var b = new TimeSlot(At(10), At(11));
        Assert.False(a.Overlaps(b));
    }

    [Fact]
    public void End_must_be_after_start()
    {
        Assert.Throws<ArgumentException>(() => new TimeSlot(At(11), At(9)));
    }

    [Fact]
    public void FromDuration_computes_end()
    {
        var slot = TimeSlot.FromDuration(At(9), 90);
        Assert.Equal(At(9).AddMinutes(90), slot.End);
        Assert.Equal(TimeSpan.FromMinutes(90), slot.Duration);
    }
}

public class VerticalConfigTests
{
    [Fact]
    public void Round_trips_through_json_with_snake_case()
    {
        var config = new VerticalConfig
        {
            ResourceLabel = "Table",
            ResourceLabelPlural = "Tables",
            DepositThresholdPartySize = 6,
            DepositPerHeadCents = 1000
        };

        var json = config.ToJson();
        Assert.Contains("resource_label_plural", json);
        Assert.Contains("deposit_threshold_party_size", json);

        var restored = VerticalConfig.FromJson(json);
        Assert.Equal(config.ResourceLabelPlural, restored.ResourceLabelPlural);
        Assert.Equal(6, restored.DepositThresholdPartySize);
    }

    [Fact]
    public void FromJson_handles_empty_input()
    {
        var config = VerticalConfig.FromJson("");
        Assert.Equal("Resource", config.ResourceLabel);
    }
}
