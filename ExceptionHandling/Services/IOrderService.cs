using ExceptionHandling.DTOs;
using ExceptionHandling.Models;

namespace ExceptionHandling.Services;
public interface IOrderService
{        
    public Task<Order> GetByIdAsync(string id, CancellationToken ct);
    public Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken ct);
    public Task DeleteAsync(string id, CancellationToken ct);
}
