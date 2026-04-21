using APIVersioning.Models;

namespace APIVersioning.Services
{
    public interface IOrderService
    {
        public Task<(IEnumerable<Order> Orders, int Total)> GetPagedAsync(
            int page, int pageSize, CancellationToken ct);
        public Task<Order?> GetByIdAsync(string id, CancellationToken ct);
        public Task<bool> CancelAsync(string id, CancellationToken ct);
        public Task<Order> CreateV2Async(DTOs.V2.CreateOrderRequest request, CancellationToken ct);
        public Task<Order> CreateAsync(string customerId, List<(string ProductId, int Quantity, decimal UnitPrice)> list, CancellationToken ct);
        public Task<(IEnumerable<Order> Orders, string nextCursor, string prevCursor)> GetCursorPagedAsync(string? cursor, int limit, string? status, CancellationToken ct);
        public Task<Order> CreateV3Async(DTOs.V3.CreateOrderRequest request, string idempotencyKey, CancellationToken ct);
        public Task<Order> RefundAsync(string id, decimal? amount, CancellationToken ct);

    }   

}
