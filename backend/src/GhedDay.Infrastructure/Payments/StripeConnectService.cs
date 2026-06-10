using GhedDay.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace GhedDay.Infrastructure.Payments;

/// <summary>
/// Stripe Connect Express onboarding links for business owners (Phase 3).
/// </summary>
public sealed class StripeConnectService
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripeConnectService> _logger;

    public StripeConnectService(IOptions<StripeOptions> options, ILogger<StripeConnectService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates an Express connected account (if needed) and returns a hosted onboarding URL.
    /// Returns null when Stripe is not configured.
    /// </summary>
    public async Task<ConnectOnboardingResult?> CreateOnboardingLinkAsync(
        string? existingAccountId,
        string refreshUrl,
        string returnUrl,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            return null;

        StripeConfiguration.ApiKey = _options.SecretKey;

        try
        {
            var accountId = existingAccountId;
            if (string.IsNullOrWhiteSpace(accountId))
            {
                var account = await new AccountService().CreateAsync(new AccountCreateOptions
                {
                    Type = "express",
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                        Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                    },
                }, cancellationToken: ct);
                accountId = account.Id;
            }

            var link = await new AccountLinkService().CreateAsync(new AccountLinkCreateOptions
            {
                Account = accountId,
                RefreshUrl = refreshUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding",
            }, cancellationToken: ct);

            return new ConnectOnboardingResult(accountId!, link.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe Connect onboarding link.");
            return null;
        }
    }
}

public sealed record ConnectOnboardingResult(string AccountId, string OnboardingUrl);
