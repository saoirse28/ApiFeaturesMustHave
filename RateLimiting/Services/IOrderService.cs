using RateLimiting.DTOs;
using RateLimiting.Model;

namespace RateLimiting.Services
{
    public interface IOrderService
    {
        public Task<IEnumerable<Order>> GetListAsync(CancellationToken ct);
        public Task<Order> GetByIdAsync(string id, CancellationToken ct);
        public Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken ct);
        public Task<IEnumerable<Order>> BulkCreateAsync(IEnumerable<CreateOrderRequest> requests, CancellationToken ct);
        public Task<IEnumerable<Order>> ExportAllAsync(CancellationToken ct);
    }
}
