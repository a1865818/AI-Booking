using GhedDay.Domain.Entities;
using GhedDay.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GhedDay.Infrastructure.Payments;

/// <summary>
/// Stripe Checkout Session creation for booking deposits (Phase 3). Returns a hosted payment URL
/// the customer receives over SMS after a hold is placed.
/// </summary>
public sealed class StripeService
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IOptions<StripeOptions> options, ILogger<StripeService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates a one-time Checkout Session for a booking deposit. Returns null when Stripe is not
    /// configured or the amount is zero.
    /// </summary>
    public async Task<DepositPaymentResult?> CreateDepositPaymentAsync(
        Business business,
        Guid bookingId,
        int amountCents,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey) || amountCents <= 0)
            return null;

        StripeConfiguration.ApiKey = _options.SecretKey;

        var metadata = new Dictionary<string, string>
        {
            ["booking_id"] = bookingId.ToString(),
            ["business_id"] = business.Id.ToString(),
        };

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = _options.SuccessUrl,
            CancelUrl = _options.CancelUrl,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = amountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{business.Name} booking deposit",
                        },
                    },
                },
            ],
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = metadata,
            },
            Metadata = metadata,
        };

        RequestOptions? requestOptions = null;
        if (!string.IsNullOrWhiteSpace(business.StripeAccountId))
            requestOptions = new RequestOptions { StripeAccount = business.StripeAccountId };

        try
        {
            var session = await new SessionService().CreateAsync(sessionOptions, requestOptions, cancellationToken: ct);
            if (string.IsNullOrWhiteSpace(session.Url) || string.IsNullOrWhiteSpace(session.PaymentIntentId))
            {
                _logger.LogWarning("Stripe session {SessionId} missing URL or payment intent.", session.Id);
                return null;
            }

            return new DepositPaymentResult(session.PaymentIntentId, session.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe checkout for booking {BookingId}.", bookingId);
            return null;
        }
    }
}

public sealed record DepositPaymentResult(string PaymentIntentId, string PaymentUrl);
