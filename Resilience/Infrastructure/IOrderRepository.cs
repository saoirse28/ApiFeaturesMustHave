using Resilience.DTOs;

namespace Resilience.Infrastructure
{
    public interface IOrderRepository
    {
        Task<Order?> FindByIdAsync(string id, CancellationToken ct = default);
        Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId, CancellationToken ct = default);
        Task SaveAsync(Order order, CancellationToken ct = default);
        Task UpdateStatusAsync(string orderId, string status, CancellationToken ct = default);
    }
}