using GhedDay.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GhedDay.Infrastructure.Payments;

/// <summary>
/// Stripe PaymentIntent creation for booking deposits (Phase 3). A PaymentIntent is created
/// in <c>CreateBookingHoldTool</c> only when <c>IVerticalConfigService.RequiresDeposit</c>
/// returns true.
/// </summary>
public sealed class StripeService
{
    private readonly StripeOptions _options;

    public StripeService(IOptions<StripeOptions> options) => _options = options.Value;

    // Phase 3: create PaymentIntent on the connected account, return client secret / hosted URL.
}
