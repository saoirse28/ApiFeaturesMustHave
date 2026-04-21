using ExceptionHandling.DTOs;
using ExceptionHandling.Exceptions;
using ExceptionHandling.Models;
using ExceptionHandling.Repository;
using static ExceptionHandling.Services.OrderService;

namespace ExceptionHandling.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public OrderService(IOrderRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<Order> GetByIdAsync(string id, CancellationToken ct)
    {
        var order = await _repo.FindByIdAsync(id, ct);

        if (order is null)
            throw new NotFoundException(nameof(Order), id);

        if (order.OwnerId != _currentUser.UserId && !_currentUser.IsAdmin)
            throw new ForbiddenException(nameof(Order));

        return order;
    }

    public async Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        // Manual field-level validation
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.CustomerId))
            errors.Add(new ValidationError("customerId", "Customer ID is required."));

        if (request.Items is null || request.Items.Count == 0)
            errors.Add(new ValidationError("items", "At least one item is required."));

        if (request.Items?.Any(i => i.Quantity <= 0) == true)
            errors.Add(new ValidationError("items[].quantity", "Quantity must be greater than zero.", "INVALID_QUANTITY"));

        if (errors.Count > 0)
            throw new ValidationException(errors);

        // Business rule: check for duplicate order
        var existing = await _repo.FindByReferenceAsync(request.Reference, ct);
        if (existing is not null)
            throw new ConflictException(nameof(Order), $"Reference '{request.Reference}' already exists.");

        var order = new Order(request);
        await _repo.SaveAsync(order, ct);
        return order;
    }

    public async Task DeleteAsync(string id, CancellationToken ct)
    {
        var order = await _repo.FindByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Order), id);

        var isOwner = order.OwnerId.Equals(_currentUser.UserId);
        if (!isOwner && !_currentUser.IsAdmin)
            throw new ForbiddenException(nameof(Order));

        if (order.Status == OrderStatus.Shipped)
            throw new ConflictException(nameof(Order), "Cannot delete a shipped order.");

        await _repo.DeleteAsync(id, ct);
    }
        
}