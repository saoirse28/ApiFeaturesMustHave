using ProperLogging.DTOs;
using ProperLogging.Models;

namespace ProperLogging.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);
        Task<Order?> GetOrderAsync(string orderId, CancellationToken ct = default);
        Task<PaymentResult> ProcessPaymentAsync(string orderId, PaymentRequest request, CancellationToken ct = default);
    }
}