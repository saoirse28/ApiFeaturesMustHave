using APIVersioning.Models;

namespace APIVersioning.Services
{
    public class OrderService : IOrderService
    {
        public Task<bool> CancelAsync(string id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<Order> CreateAsync(string customerId, List<(string ProductId, int Quantity, decimal UnitPrice)> list, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<Order> CreateV2Async(DTOs.V2.CreateOrderRequest request, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<Order> CreateV3Async(DTOs.V3.CreateOrderRequest request, string idempotencyKey, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<Order?> GetByIdAsync(string id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<(IEnumerable<Order> Orders, string nextCursor, string prevCursor)> GetCursorPagedAsync(string? cursor, int limit, string? status, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<(IEnumerable<Order> Orders, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<Order> RefundAsync(string id, decimal? amount, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
