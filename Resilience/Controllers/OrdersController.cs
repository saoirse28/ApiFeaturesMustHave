using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Polly.CircuitBreaker;
using Resilience.Domain.Exceptions;
using Resilience.HttpClients;
using Resilience.Models;
using Resilience.RateLimiting;
using Resilience.Resilience;
using Resilience.Services;
using ResilienceDemo.HttpClients;
using System.Net;

namespace Resilience.Controllers;

/// <summary>
/// Orders API controller demonstrating resilience patterns at the HTTP layer.
///
/// Resilience responsibilities at each level:
///   - Controller  → catches BrokenCircuitException, maps to 503
///   - Service     → orchestrates use cases, catches domain-specific failures
///   - Repository  → Polly DB pipeline (retry + circuit breaker + timeout)
///   - HttpClients → Polly HTTP pipeline (retry + circuit breaker + timeout)
///
/// The controller NEVER catches HttpRequestException or TimeoutRejectedException
/// directly — those are handled by the Polly pipeline inside the typed clients.
/// The only resilience concern at controller level is BrokenCircuitException,
/// which signals that a downstream service is known-down and should return 503.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
[Produces("application/json")]
[EnableRateLimiting(RateLimitPolicies.PerClient)]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly InventoryClient _inventoryClient;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orderService,
        InventoryClient inventoryClient,
        ILogger<OrdersController> logger)
    {
        _orderService    = orderService;
        _inventoryClient = inventoryClient;
        _logger          = logger;
    }

    // ── GET /api/v1/orders ────────────────────────────────────────────────────

    /// <summary>Returns a paginated list of orders for the authenticated customer.</summary>
    /// <param name="page">Page number (1-based). Default: 1.</param>
    /// <param name="pageSize">Items per page (max 100). Default: 20.</param>
    [HttpGet]
    [ProducesResponseType<PagedOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _orderService.GetListAsync(page, pageSize, ct);
            return Ok(result);
        }
        catch (BrokenCircuitException ex)
        {
            // Database circuit is open — fail fast with a clear 503
            _logger.LogWarning(ex,
                "GET /orders failed — database circuit open");

            return ServiceUnavailable(
                "Order history is temporarily unavailable. Please try again shortly.",
                "DATABASE_CIRCUIT_OPEN");
        }
    }

    // ── GET /api/v1/orders/{id} ───────────────────────────────────────────────

    /// <summary>Returns a single order by ID.</summary>
    /// <param name="id">Order identifier.</param>
    [HttpGet("{id}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetById(
        string id,
        CancellationToken ct)
    {
        try
        {
            var order = await _orderService.GetByIdAsync(id, ct);

            return order is null
                ? NotFound(Problem(
                    title: "Order not found",
                    detail: $"Order '{id}' does not exist.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://api.myapp.com/errors/not-found"))
                : Ok(order);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex,
                "GET /orders/{OrderId} failed — database circuit open", id);

            return ServiceUnavailable(
                "Order details are temporarily unavailable. Please try again shortly.",
                "DATABASE_CIRCUIT_OPEN");
        }
    }

    // ── POST /api/v1/orders ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a new order.
    ///
    /// Resilience behaviour:
    ///   - Payment failures are retried up to 3 times with exponential backoff.
    ///   - If the payment circuit is open, returns 503 immediately (fail fast).
    ///   - If the inventory service is degraded, the order proceeds optimistically.
    ///   - The Idempotency-Key header makes retries safe — duplicate keys return
    ///     the original response without re-processing.
    /// </summary>
    /// <remarks>
    /// Always supply a unique UUID in the Idempotency-Key header.
    /// Repeating the same key within 24 hours returns the cached response.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType<Models.CreateOrderResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        // Validate idempotency key — required to make payment retries safe
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(Problem(
                title: "Idempotency-Key required",
                detail: "Include a unique Idempotency-Key header to safely retry this request.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://api.myapp.com/errors/missing-idempotency-key"));
        }

        try
        {
            var result = await _orderService.CreateOrderAsync(
                request, idempotencyKey, ct);

            return result.Succeeded
                ? CreatedAtAction(nameof(GetById),
                    new { id = result.Order!.Id },
                    result)
                : UnprocessableEntity(Problem(
                    title: "Order creation failed",
                    detail: result.ErrorMessage,
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    type: "https://api.myapp.com/errors/order-creation-failed"));
        }
        catch (BrokenCircuitException ex) when (IsPaymentCircuit(ex))
        {
            _logger.LogError(ex,
                "POST /orders — payment circuit open, idempotency key: {Key}",
                idempotencyKey);

            return ServiceUnavailable(
                "Payment processing is temporarily unavailable. " +
                "Your order has been saved — retry payment using the same Idempotency-Key.",
                "PAYMENT_CIRCUIT_OPEN",
                retryAfterSeconds: 30);
        }
        catch (BrokenCircuitException ex) when (IsDatabaseCircuit(ex))
        {
            _logger.LogError(ex,
                "POST /orders — database circuit open");

            return ServiceUnavailable(
                "Order service is temporarily unavailable. Please try again shortly.",
                "DATABASE_CIRCUIT_OPEN",
                retryAfterSeconds: 15);
        }
    }

    // ── PUT /api/v1/orders/{id} ───────────────────────────────────────────────

    /// <summary>Updates an existing order (pre-shipment only).</summary>
    [HttpPut("{id}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateOrderRequest request,
        CancellationToken ct)
    {
        try
        {
            var order = await _orderService.UpdateAsync(id, request, ct);
            return Ok(order);
        }
        catch (OrderNotFoundException)
        {
            return NotFound(Problem(
                title: "Order not found",
                detail: $"Order '{id}' does not exist.",
                statusCode: StatusCodes.Status404NotFound,
                type: "https://api.myapp.com/errors/not-found"));
        }
        catch (OrderAlreadyShippedException)
        {
            return Conflict(Problem(
                title: "Order cannot be modified",
                detail: $"Order '{id}' has already been shipped and cannot be updated.",
                statusCode: StatusCodes.Status409Conflict,
                type: "https://api.myapp.com/errors/order-already-shipped"));
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex,
                "PUT /orders/{OrderId} — circuit open", id);

            return ServiceUnavailable(
                "Order update is temporarily unavailable. Please try again shortly.",
                "CIRCUIT_OPEN");
        }
    }

    // ── DELETE /api/v1/orders/{id} ────────────────────────────────────────────

    /// <summary>Cancels an order that has not yet been shipped.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Cancel(
        string id,
        CancellationToken ct)
    {
        try
        {
            await _orderService.CancelAsync(id, ct);
            return NoContent();
        }
        catch (OrderNotFoundException)
        {
            return NotFound(Problem(
                title: "Order not found",
                detail: $"Order '{id}' does not exist.",
                statusCode: StatusCodes.Status404NotFound));
        }
        catch (OrderAlreadyShippedException)
        {
            return Conflict(Problem(
                title: "Cannot cancel shipped order",
                detail: $"Order '{id}' has already been shipped.",
                statusCode: StatusCodes.Status409Conflict,
                type: "https://api.myapp.com/errors/order-already-shipped"));
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex,
                "DELETE /orders/{OrderId} — circuit open", id);

            return ServiceUnavailable(
                "Order cancellation is temporarily unavailable. Please try again shortly.",
                "CIRCUIT_OPEN");
        }
    }

    // ── POST /api/v1/orders/{id}/retry-payment ────────────────────────────────

    /// <summary>
    /// Retries a failed payment on an existing order.
    ///
    /// Resilience behaviour:
    ///   - Polly retries the payment call up to 3x with exponential backoff.
    ///   - Uses the original Idempotency-Key to prevent duplicate charges.
    ///   - Returns 503 immediately if the payment circuit is open.
    /// </summary>
    [HttpPost("{id}/retry-payment")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [EnableRateLimiting(RateLimitPolicies.SensitiveOperation)]
    public async Task<IActionResult> RetryPayment(
        string id,
        CancellationToken ct)
    {
        try
        {
            var order = await _orderService.RetryPaymentAsync(id, ct);
            return Ok(order);
        }
        catch (OrderNotFoundException)
        {
            return NotFound(Problem(
                title: "Order not found",
                detail: $"Order '{id}' does not exist.",
                statusCode: StatusCodes.Status404NotFound));
        }
        catch (OrderPaymentAlreadySucceededException)
        {
            return Conflict(Problem(
                title: "Payment already succeeded",
                detail: $"Order '{id}' has already been paid.",
                statusCode: StatusCodes.Status409Conflict,
                type: "https://api.myapp.com/errors/payment-already-succeeded"));
        }
        catch (BrokenCircuitException ex) when (IsPaymentCircuit(ex))
        {
            _logger.LogWarning(ex,
                "POST /orders/{OrderId}/retry-payment — payment circuit open", id);

            return ServiceUnavailable(
                "Payment service is temporarily unavailable. Please try again in 30 seconds.",
                "PAYMENT_CIRCUIT_OPEN",
                retryAfterSeconds: 30);
        }
    }

    // ── GET /api/v1/orders/{id}/stock-check ───────────────────────────────────

    /// <summary>
    /// Checks inventory stock level for all items in an order.
    ///
    /// Resilience behaviour:
    ///   - If the inventory circuit is open, returns a degraded response
    ///     (assumed available) rather than failing the entire check.
    ///   - Clients should treat isEstimated=true as "check later".
    /// </summary>
    [HttpGet("{id}/stock-check")]
    [ProducesResponseType<StockCheckResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckStock(
        string id,
        CancellationToken ct)
    {
        var order = await _orderService.GetByIdAsync(id, ct);
        if (order is null)
            return NotFound();

        var stockLevels = new List<ItemStockLevel>();
        var isDegraded = false;

        foreach (var item in order.Items)
        {
            // InventoryClient already handles BrokenCircuitException internally —
            // it returns a degraded StockLevel(available: true, isEstimated: true)
            // rather than letting the exception propagate here.
            var stock = await _inventoryClient
                .GetStockLevelAsync(item.ProductId, ct);

            if (stock is null)
            {
                stockLevels.Add(new ItemStockLevel(
                    item.ProductId,
                    Available: false,
                    Quantity: 0,
                    IsEstimated: false));
            }
            else
            {
                if (stock.IsEstimated) isDegraded = true;

                stockLevels.Add(new ItemStockLevel(
                    item.ProductId,
                    Available: stock.Available,
                    Quantity: stock.Quantity,
                    IsEstimated: stock.IsEstimated));
            }
        }

        return Ok(new StockCheckResult(
            OrderId: id,
            Items: stockLevels,
            IsDegraded: isDegraded,
            CheckedAt: DateTimeOffset.UtcNow));
    }

    // ── GET /api/v1/orders/circuit-status ─────────────────────────────────────

    /// <summary>
    /// Returns the current circuit breaker state for all downstream services.
    /// Used by health dashboards and ops runbooks.
    /// Protected — Admin only.
    /// </summary>
    [HttpGet("circuit-status")]
    [Authorize(Roles = "Admin")]
    [DisableRateLimiting]
    [ProducesResponseType<CircuitStatusResponse>(StatusCodes.Status200OK)]
    public IActionResult GetCircuitStatus(
        [FromServices] ICircuitBreakerStateProvider circuitState)
    {
        return Ok(new CircuitStatusResponse(
            Payment: circuitState.GetState("payment-pipeline"),
            Inventory: circuitState.GetState("inventory-pipeline"),
            Database: circuitState.GetState("database-pipeline"),
            Redis: circuitState.GetState("redis-pipeline"),
            CheckedAt: DateTimeOffset.UtcNow));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds a structured 503 ProblemDetails response with Retry-After header.
    /// </summary>
    private ObjectResult ServiceUnavailable(
        string detail,
        string errorCode,
        int retryAfterSeconds = 60)
    {
        Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new
            {
                type = $"https://api.myapp.com/errors/{errorCode.ToLower().Replace('_', '-')}",
                title = "Service Temporarily Unavailable",
                status = 503,
                detail,
                errorCode,
                retryAfterSeconds,
                correlationId = HttpContext.Items["CorrelationId"]?.ToString()
                             ?? HttpContext.TraceIdentifier,
                timestamp = DateTimeOffset.UtcNow
            });
    }

    /// <summary>
    /// Determines whether the BrokenCircuitException originated
    /// from the payment pipeline.
    /// </summary>
    private static bool IsPaymentCircuit(BrokenCircuitException ex) =>
        ex.Message.Contains("payment", StringComparison.OrdinalIgnoreCase) ||
        ex.StackTrace?.Contains("PaymentClient",
            StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Determines whether the BrokenCircuitException originated
    /// from the database pipeline.
    /// </summary>
    private static bool IsDatabaseCircuit(BrokenCircuitException ex) =>
        ex.Message.Contains("database", StringComparison.OrdinalIgnoreCase) ||
        ex.StackTrace?.Contains("OrderRepository",
            StringComparison.OrdinalIgnoreCase) == true;
}