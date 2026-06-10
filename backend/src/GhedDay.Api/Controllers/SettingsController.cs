using GhedDay.Application.Common;
using GhedDay.Application.Verticals;
using GhedDay.Domain.Entities;
using GhedDay.Domain.ValueObjects;
using GhedDay.Infrastructure.Configuration;
using GhedDay.Infrastructure.Data;
using GhedDay.Infrastructure.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GhedDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class SettingsController : ControllerBase
{
    private readonly GhedDayDbContext _db;
    private readonly IVerticalConfigService _verticalConfig;
    private readonly ITenantContext _tenant;
    private readonly StripeConnectService _stripeConnect;
    private readonly StripeOptions _stripeOptions;

    public SettingsController(
        GhedDayDbContext db,
        IVerticalConfigService verticalConfig,
        ITenantContext tenant,
        StripeConnectService stripeConnect,
        IOptions<StripeOptions> stripeOptions)
    {
        _db = db;
        _verticalConfig = verticalConfig;
        _tenant = tenant;
        _stripeConnect = stripeConnect;
        _stripeOptions = stripeOptions.Value;
    }

    /// <summary>Current tenant settings, vertical config, hours, and persona.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var businessId = _tenant.RequireBusinessId();
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, ct);
        if (business is null)
            return NotFound();

        var hours = await _db.BusinessHours
            .Where(h => h.BusinessId == businessId)
            .OrderBy(h => h.DayOfWeek)
            .Select(h => new
            {
                dayOfWeek = h.DayOfWeek,
                open = h.OpenTime.ToString("HH:mm"),
                close = h.CloseTime.ToString("HH:mm"),
            })
            .ToListAsync(ct);

        var settings = business.GetSettings();

        return Ok(new
        {
            business.Id,
            business.Name,
            business.Timezone,
            businessType = business.BusinessType.ToString(),
            resourceLabel = _verticalConfig.GetResourceLabel(business),
            resourceLabelPlural = _verticalConfig.GetResourceLabel(business, plural: true),
            verticalConfig = business.GetVerticalConfig(),
            settings = new { aiPersona = settings.AiPersona },
            hours,
            stripeConnected = !string.IsNullOrWhiteSpace(business.StripeAccountId),
        });
    }

    [HttpPut("persona")]
    public async Task<IActionResult> UpdatePersona([FromBody] UpdatePersonaRequest request, CancellationToken ct)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == _tenant.RequireBusinessId(), ct);
        if (business is null)
            return NotFound();

        business.SetSettings(new BusinessSettings { AiPersona = request.AiPersona?.Trim() });
        await _db.SaveChangesAsync(ct);

        return Ok(new { aiPersona = business.GetSettings().AiPersona });
    }

    [HttpPut("deposit")]
    public async Task<IActionResult> UpdateDeposit([FromBody] UpdateDepositRequest request, CancellationToken ct)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == _tenant.RequireBusinessId(), ct);
        if (business is null)
            return NotFound();

        var config = business.GetVerticalConfig();
        config = config with
        {
            DepositCents = request.DepositCents ?? config.DepositCents,
            HoldMinutes = request.HoldMinutes ?? config.HoldMinutes,
            DepositThresholdPartySize = request.DepositThresholdPartySize ?? config.DepositThresholdPartySize,
            DepositPerHeadCents = request.DepositPerHeadCents ?? config.DepositPerHeadCents,
        };

        business.SetVerticalConfig(config);
        await _db.SaveChangesAsync(ct);

        return Ok(new { verticalConfig = business.GetVerticalConfig() });
    }

    [HttpPut("hours")]
    public async Task<IActionResult> UpdateHours([FromBody] UpdateHoursRequest request, CancellationToken ct)
    {
        var businessId = _tenant.RequireBusinessId();

        if (request.Hours is null || request.Hours.Count == 0)
            return BadRequest(new { error = "At least one hours row is required." });

        var existing = await _db.BusinessHours.Where(h => h.BusinessId == businessId).ToListAsync(ct);
        _db.BusinessHours.RemoveRange(existing);

        foreach (var row in request.Hours)
        {
            if (!TimeOnly.TryParse(row.Open, out var open) || !TimeOnly.TryParse(row.Close, out var close))
                return BadRequest(new { error = $"Invalid time for day {row.DayOfWeek}." });

            if (row.DayOfWeek is < 0 or > 6)
                return BadRequest(new { error = $"Invalid day_of_week {row.DayOfWeek}." });

            _db.BusinessHours.Add(new BusinessHours
            {
                BusinessId = businessId,
                DayOfWeek = row.DayOfWeek,
                OpenTime = open,
                CloseTime = close,
            });
        }

        await _db.SaveChangesAsync(ct);

        var hours = await _db.BusinessHours
            .Where(h => h.BusinessId == businessId)
            .OrderBy(h => h.DayOfWeek)
            .Select(h => new { dayOfWeek = h.DayOfWeek, open = h.OpenTime.ToString("HH:mm"), close = h.CloseTime.ToString("HH:mm") })
            .ToListAsync(ct);

        return Ok(new { hours });
    }

    [HttpPost("stripe-connect")]
    public async Task<IActionResult> StartStripeConnect(CancellationToken ct)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == _tenant.RequireBusinessId(), ct);
        if (business is null)
            return NotFound();

        var onboarding = await _stripeConnect.CreateOnboardingLinkAsync(
            business.StripeAccountId,
            refreshUrl: _stripeOptions.CancelUrl,
            returnUrl: _stripeOptions.SuccessUrl,
            ct);

        if (onboarding is null)
            return BadRequest(new { error = "Stripe is not configured on this server." });

        if (string.IsNullOrWhiteSpace(business.StripeAccountId))
        {
            business.StripeAccountId = onboarding.AccountId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { onboardingUrl = onboarding.OnboardingUrl });
    }

    public sealed record UpdatePersonaRequest(string? AiPersona);

    public sealed record UpdateDepositRequest(
        int? DepositCents,
        int? HoldMinutes,
        int? DepositThresholdPartySize,
        int? DepositPerHeadCents);

    public sealed record UpdateHoursRequest(IReadOnlyList<HourRow> Hours);

    public sealed record HourRow(int DayOfWeek, string Open, string Close);
}
