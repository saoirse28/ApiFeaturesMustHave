using Microsoft.AspNetCore.Mvc;
using ProperLogging.DTOs;
using ProperLogging.Logging;
using ProperLogging.Services;
using Serilog.Context;

namespace ProperLogging.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orders,
        ILogger<OrdersController> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        // Push CustomerId into log context for this entire controller action
        using (LogContext.PushProperty(LoggingConstants.CustomerId, request.CustomerId))
        {
            var order = await _orders.CreateOrderAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var order = await _orders.GetOrderAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("{id}/pay")]
    public async Task<IActionResult> Pay(
        string id,
        [FromBody] PaymentRequest request,
        CancellationToken ct)
    {
        using (LogContext.PushProperty(LoggingConstants.OrderId, id))
        using (LogContext.PushProperty(LoggingConstants.PaymentMethod, request.Method))
        {
            var result = await _orders.ProcessPaymentAsync(id, request, ct);
            return Ok(result);
        }
    }
}
