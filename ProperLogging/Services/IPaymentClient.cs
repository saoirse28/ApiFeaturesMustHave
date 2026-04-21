using ProperLogging.DTOs;

namespace ProperLogging.Services
{
    public interface IPaymentClient
    {
        Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default);
    }
}