
using ProperLogging.Models;

namespace ProperLogging.Repository;

public interface IOrderRepository
{
    public Task<Order> FindByIdAsync(string id, CancellationToken ct);
    public Task<Order> FindByReferenceAsync(string reference, CancellationToken ct);
    public Task DeleteAsync(string id, CancellationToken ct);
    public Task SaveAsync(Order order, CancellationToken ct);

}
