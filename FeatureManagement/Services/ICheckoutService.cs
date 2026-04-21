using FeatureManagement.DTOs;

namespace FeatureManagement.Services
{
    public interface ICheckoutService
    {
        public Task<CheckoutResult> CheckoutAsync(Cart cart, CancellationToken ct);
        public Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, CancellationToken ct);
        public Task<CheckoutResult> QuickCheckoutAsync(OneClickRequest request, CancellationToken ct);
        public Task<IReadOnlyList<PaymentOption>> GetPaymentOptionsAsync(CancellationToken ct);
    }
}
