using FeatureManagement.DTOs;

namespace FeatureManagement.Services
{
    public class LegacyCheckoutService
    {
        public Task<CheckoutResult> CheckoutAsync(Cart cart, CancellationToken ct)
        {
            // Legacy checkout logic goes here
            return Task.FromResult(new CheckoutResult());
        }
    }
}
