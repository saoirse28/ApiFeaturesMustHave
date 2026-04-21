using Polly.CircuitBreaker;
using Resilience.HttpClients;
using Resilience.Infrastructure;
using Resilience.Models;
using Order = Resilience.DTOs.Order;

namespace Resilience.Services;

/// <summary>
/// Orchestrates order creation using multiple resilient downstream clients.
/// Demonstrates how BrokenCircuitException should be caught at the orchestration
/// layer to implement graceful degradation at the business level.
/// </summary>
public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;
    private readonly PaymentClient _payments;
    private readonly InventoryClient _inventory;
    private readonly NotificationClient _notifications;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository repo,
        PaymentClient payments,
        InventoryClient inventory,
        NotificationClient notifications,
        ILogger<OrderService> logger)
    {
        _repo          = repo;
        _payments      = payments;
        _inventory     = inventory;
        _notifications = notifications;
        _logger        = logger;
    }

    public Task CancelAsync(string id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<CreateOrderResult> CreateOrderAsync(
        CreateOrderRequest request,
        string? idempotencyKey,
        CancellationToken ct = default)
    {
        // Step 1: Check stock — degradable if inventory service is down
        var stockLevel = await _inventory.GetStockLevelAsync(request.ProductId, ct);

        if (stockLevel is { Available: false })
        {
            _logger.LogWarning(
                "Product {ProductId} is out of stock — rejecting order",
                request.ProductId);
            return CreateOrderResult.Failure("Product is out of stock");
        }

        // Step 2: Persist the order
        var order = Order.Create(request);

        try
        {
            await _repo.SaveAsync(order, ct);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex,
                "Database circuit open — cannot persist order {OrderId}", order.Id);
            return CreateOrderResult.Failure(
                "Service temporarily unavailable. Please try again shortly.");
        }

        // Step 3: Charge payment — critical, cannot degrade
        try
        {
            var chargeResult = await _payments.ChargeAsync(
                new DTOs.ChargeRequest
                {
                    OrderId        = order.Id,
                    Amount         = order.Total,
                    Currency       = "USD",
                    IdempotencyKey = $"charge-{order.Id}" // safe retry key
                },
                ct);

            await _repo.UpdateStatusAsync(order.Id, "PaymentConfirmed", ct);

            // Step 4: Send notification — non-critical, fire and forget
            _ = Task.Run(() =>
                _notifications.SendOrderConfirmationAsync(order.Id, request.UserId),
                CancellationToken.None);  // intentionally not propagating cancellation

            _logger.LogInformation(
                "Order {OrderId} created successfully — transaction {TransactionId}",
                order.Id, chargeResult.TransactionId);

            return CreateOrderResult.Success(order, chargeResult.TransactionId);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex,
                "Payment circuit open — order {OrderId} cannot be charged", order.Id);
            await _repo.UpdateStatusAsync(order.Id, "PaymentFailed", ct);
            return CreateOrderResult.Failure(
                "Payment service is temporarily unavailable. Order saved — you can retry payment.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Payment request failed for order {OrderId} after all retries", order.Id);
            await _repo.UpdateStatusAsync(order.Id, "PaymentFailed", ct);
            return CreateOrderResult.Failure("Payment processing failed. Please try again.");
        }
    }

    public Task<OrderDto> GetByIdAsync(string id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<OrderDto>> GetListAsync(int page, int pageSize, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<OrderDto> RetryPaymentAsync(string id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<OrderDto> UpdateAsync(string id, UpdateOrderRequest request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}