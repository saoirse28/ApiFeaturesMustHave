using ProperLogging.DTOs;

namespace ProperLogging.Services
{
    public class PaymentClient : IPaymentClient
    {
        public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default)
        {
            return Task.FromResult(new PaymentResult
            {
                TransactionId = Guid.NewGuid().ToString()
            });
        }
    }
}
