using RateLimiting.DTOs;
using RateLimiting.Model;

namespace RateLimiting.Services
{
    public class OrderService : IOrderService
    {
        public Task<IEnumerable<Order>> BulkCreateAsync(IEnumerable<CreateOrderRequest> requests, CancellationToken ct)
        {
            return Task.FromResult(
                requests
                .Select(r => 
                    new Order { Id = new Random().Next() }
                ));
        }

        public Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken ct)
        {
            return Task.FromResult(
                new Order { Id = new Random().Next() }
            );
        }

        public Task<IEnumerable<Order>> ExportAllAsync(CancellationToken ct)
        {
            return Task.FromResult(
                Enumerable.Range(1, 10).Select(i => new Order { Id = i })
            );
        }

        public Task<Order> GetByIdAsync(string id, CancellationToken ct)
        {
            return Task.FromResult( new Order { Id = new Random().Next() });
        }

        public Task<IEnumerable<Order>> GetListAsync(CancellationToken ct)
        {
            return Task.FromResult(
                Enumerable.Range(1, 10).Select(i => new Order { Id = i })
            );
        }
    }
}
