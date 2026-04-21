using ProperLogging.DTOs;
using ProperLogging.Exceptions;
using ProperLogging.Logging;
using ProperLogging.Models;
using ProperLogging.Repository;
using System.Diagnostics;

namespace ProperLogging.Services;

/// <summary>
/// Demonstrates proper logging patterns inside a domain service:
///   - Use source-generated [LoggerMessage] methods (zero-alloc).
///   - Log structured properties, not interpolated strings.
///   - Use correct log levels for each scenario.
///   - Add scoped context with LogContext.PushProperty for operation-wide fields.
///   - Never log PII (passwords, card numbers, SSNs).
/// </summary>
public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;
    private readonly IPaymentClient _payments;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository repo,
        IPaymentClient payments,
        ILogger<OrderService> logger)
    {
        _repo     = repo;
        _payments = payments;
        _logger   = logger;
    }

    public async Task<Order> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken ct = default)
    {
        var order = new Order(request);
        var sw = Stopwatch.StartNew();

        // Push OrderId into LogContext — all log calls in this scope
        // automatically carry OrderId without repeating it manually.
        using (Serilog.Context.LogContext.PushProperty(
                   LoggingConstants.OrderId, order.Id))
        {
            try
            {
                await _repo.SaveAsync(order, ct);
                sw.Stop();

                // Source-generated — zero allocation, compile-time safe
                _logger.OrderCreated(order.Id, request.CustomerId, order.Total);

                return order;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.OrderFulfillmentFailed(order.Id, sw.ElapsedMilliseconds, ex);
                throw;
            }
        }
    }

    public async Task<PaymentResult> ProcessPaymentAsync(
        string orderId,
        PaymentRequest request,
        CancellationToken ct = default)
    {
        _logger.PaymentInitiated(orderId, request.Amount, request.Method);

        var sw = Stopwatch.StartNew();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var result = await _payments.ChargeAsync(request, ct);
                sw.Stop();

                _logger.PaymentSucceeded(orderId, result.TransactionId);
                return result;
            }
            catch (PaymentGatewayTimeoutException ex)
            {
                sw.Stop();
                _logger.PaymentGatewayTimeout(orderId, sw.ElapsedMilliseconds, ex.Gateway);

                if (attempt == 3)
                {
                    _logger.OrderPaymentFailed(orderId, ex.Gateway, "TIMEOUT", ex);
                    throw;
                }

                _logger.OrderPaymentRetried(orderId, attempt, 3, "Gateway timeout");
                await Task.Delay(TimeSpan.FromMilliseconds(300 * attempt), ct);
                sw.Restart();
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.OrderPaymentFailed(orderId, "unknown", ex.Message, ex);
                throw;
            }
        }

        throw new InvalidOperationException("Unreachable");
    }

    public async Task<Order?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        // Debug log — only emitted when LogLevel.Debug is enabled
        // Zero cost in production where minimum level is Information
        _logger.OrderCacheChecked("checking", orderId);

        var order = await _repo.FindByIdAsync(orderId, ct);

        if (order is null)
            _logger.LogWarning(                        // ad-hoc warning — no LoggerMessage needed
                "Order {OrderId} not found in repository",
                orderId);

        return order;
    }
}
