using Resilience.Models;

namespace Resilience.Services
{
    public interface IOrderService
    {
        public Task<IEnumerable<OrderDto>> GetListAsync(int page, int pageSize, CancellationToken ct);
        public Task<OrderDto> GetByIdAsync(string id, CancellationToken ct);
        public Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, string? idempotencyKey, CancellationToken ct);
        public Task<OrderDto> UpdateAsync(string id, UpdateOrderRequest request, CancellationToken ct);
        public Task CancelAsync(string id, CancellationToken ct);
        public Task<OrderDto> RetryPaymentAsync(string id, CancellationToken ct);
    }
}
