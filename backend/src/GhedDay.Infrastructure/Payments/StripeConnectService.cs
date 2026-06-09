using GhedDay.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GhedDay.Infrastructure.Payments;

/// <summary>
/// Stripe Connect Express onboarding for business owners (Phase 3). Generates account links
/// so non-technical owners can onboard quickly.
/// </summary>
public sealed class StripeConnectService
{
    private readonly StripeOptions _options;

    public StripeConnectService(IOptions<StripeOptions> options) => _options = options.Value;

    // Phase 3: create Express connected account + onboarding AccountLink.
}
