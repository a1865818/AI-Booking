using GhedDay.Application.Verticals;
using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Domain.ValueObjects;
using Xunit;

namespace GhedDay.Application.Tests;

public class VerticalConfigServiceTests
{
    private readonly VerticalConfigService _sut = new();

    private static Business NailSalon()
    {
        var b = new Business { BusinessType = BusinessType.NailSalon };
        b.SetVerticalConfig(new VerticalConfig
        {
            ResourceLabel = "Chair",
            ResourceLabelPlural = "Chairs",
            DepositRequired = true,
            HoldMinutes = 15
        });
        return b;
    }

    private static Business Restaurant()
    {
        var b = new Business { BusinessType = BusinessType.Restaurant };
        b.SetVerticalConfig(new VerticalConfig
        {
            ResourceLabel = "Table",
            ResourceLabelPlural = "Tables",
            DepositRequired = false,
            DepositThresholdPartySize = 6,
            DepositPerHeadCents = 1000,
            DefaultDurationMinutes = 90
        });
        return b;
    }

    [Fact]
    public void GetResourceLabel_returns_singular_and_plural()
    {
        var nail = NailSalon();
        Assert.Equal("Chair", _sut.GetResourceLabel(nail));
        Assert.Equal("Chairs", _sut.GetResourceLabel(nail, plural: true));

        var restaurant = Restaurant();
        Assert.Equal("Tables", _sut.GetResourceLabel(restaurant, plural: true));
    }

    [Fact]
    public void NailSalon_always_requires_deposit()
    {
        var nail = NailSalon();
        Assert.True(_sut.RequiresDeposit(nail));
        Assert.True(_sut.RequiresDeposit(nail, partySize: 1));
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(10, true)]
    public void Restaurant_requires_deposit_only_at_or_above_threshold(int partySize, bool expected)
    {
        Assert.Equal(expected, _sut.RequiresDeposit(Restaurant(), partySize));
    }

    [Fact]
    public void Restaurant_deposit_is_per_head_above_threshold()
    {
        // 8 guests * 1000 cents = 8000.
        Assert.Equal(8000, _sut.GetDepositCents(Restaurant(), partySize: 8));
        // Below threshold → no deposit.
        Assert.Equal(0, _sut.GetDepositCents(Restaurant(), partySize: 4));
    }

    [Fact]
    public void AvailabilityParams_use_party_size_as_required_capacity_for_restaurants()
    {
        var p = _sut.GetAvailabilityParams(Restaurant(), partySize: 4);
        Assert.Equal(4, p.RequiredCapacity);
        Assert.Equal(90, p.DefaultDurationMinutes);
    }

    [Fact]
    public void AvailabilityParams_default_to_single_capacity_for_service_bookings()
    {
        var p = _sut.GetAvailabilityParams(NailSalon());
        Assert.Equal(1, p.RequiredCapacity);
    }

    [Fact]
    public void Restaurant_persona_hint_asks_for_party_size()
    {
        var hint = _sut.GetClaudePersonaHint(Restaurant());
        Assert.Contains("party size", hint, StringComparison.OrdinalIgnoreCase);
    }
}
