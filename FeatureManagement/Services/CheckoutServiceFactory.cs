using FeatureManagement.DTOs;
using Microsoft.FeatureManagement;

namespace FeatureManagement.Services;

/// <summary>
/// Factory that routes to the correct checkout implementation
/// based on the NewCheckoutFlow feature flag.
///
/// Consumers inject ICheckoutService — they are completely unaware
/// that two implementations exist. The factory handles the switch.
///
/// This is the preferred pattern over injecting IFeatureManager
/// directly into every service — it keeps feature flag logic
/// in one place and makes the service easy to test.
/// </summary>
public sealed class CheckoutServiceFactory : ICheckoutService
{
    private readonly IFeatureManager _features;
    private readonly LegacyCheckoutService _legacy;
    private readonly NewCheckoutService _newCheckout;
    private readonly ILogger<CheckoutServiceFactory> _logger;

    public CheckoutServiceFactory(
        IFeatureManager features,
        LegacyCheckoutService legacy,
        NewCheckoutService newCheckout,
        ILogger<CheckoutServiceFactory> logger)
    {
        _features    = features;
        _legacy      = legacy;
        _newCheckout = newCheckout;
        _logger      = logger;
    }

    public async Task<CheckoutResult> CheckoutAsync(
        Cart cart,
        CancellationToken ct = default)
    {
        var useNewFlow = await _features.IsEnabledAsync(FeatureFlags.FeatureFlags.NewCheckoutFlow);

        _logger.LogInformation(
            "Checkout routing: {Flow} for cart {CartId}",
            useNewFlow ? "new" : "legacy",
            cart.Id);

        return useNewFlow
            ? await _newCheckout.CheckoutAsync(cart, ct)
            : await _legacy.CheckoutAsync(cart, ct);
    }

    public Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<PaymentOption>> GetPaymentOptionsAsync(
        CancellationToken ct = default)
    {
        var options = new List<PaymentOption>();

        // Base payment options always available
        options.Add(new PaymentOption("credit-card", "Credit / Debit Card"));
        options.Add(new PaymentOption("paypal", "PayPal"));

        // BNPL only shown when flag is enabled for this user
        if (await _features.IsEnabledAsync(FeatureFlags.FeatureFlags.BuyNowPayLater))
            options.Add(new PaymentOption("bnpl", "Buy Now Pay Later"));

        return options.AsReadOnly();
    }

    public Task<CheckoutResult> QuickCheckoutAsync(OneClickRequest request, CancellationToken ct)
    {
        return _newCheckout.CheckoutAsync(request.cart, ct);
    }
}