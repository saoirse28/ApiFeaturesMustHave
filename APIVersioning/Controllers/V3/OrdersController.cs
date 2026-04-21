using APIVersioning.DTOs.V3;
using APIVersioning.Models;
using APIVersioning.Services;
using APIVersioning.Versioning;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace APIVersioning.Controllers.V3;

/// <summary>
/// V3 Orders API — CURRENT. Recommended for all new integrations.
/// Breaking changes from V2:
///   - Cursor-based pagination (no more offset/page)
///   - Idempotency key required on POST
///   - RFC 7807 error responses
///   - HATEOAS-lite _links in order response
///   - Webhook events timeline
/// </summary>
[ApiController]
[ApiVersion(ApiVersions.V3)]
[Route("api/v{version:apiVersion}/orders")]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
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

    /// <summary>
    /// Returns a cursor-paginated list of orders.
    /// Pass the nextCursor from the previous response to get the next page.
    /// </summary>
    /// <param name="cursor">Opaque cursor from previous response. Omit for first page.</param>
    /// <param name="limit">Items per page (default 20, max 100).</param>
    /// <param name="status">Filter by order status.</param>
    [HttpGet]
    [ProducesResponseType<CursorPagedResponse<OrderDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var (orders, nextCursor, prevCursor) =
            await _orders.GetCursorPagedAsync(cursor, limit, status, ct);

        var dtos = orders.Select(o => MapToV3Dto(o)).ToList();

        return Ok(new CursorPagedResponse<OrderDto>(
            dtos,
            new CursorMeta(nextCursor, prevCursor, limit, HasMore: nextCursor is not null)));
    }

    /// <summary>Returns a single order by ID with full V3 detail.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null)
            return NotFound(new
            {
                type = "https://api.myapp.com/errors/not-found",
                title = "Not Found",
                status = 404,
                detail = $"Order '{id}' not found."
            });

        return Ok(MapToV3Dto(order));
    }

    /// <summary>
    /// Creates a new order. Idempotency-Key header is required
    /// to safely retry failed requests without duplicate charges.
    /// </summary>
    /// <remarks>
    /// Supply a unique UUID in Idempotency-Key. Repeating the same key
    /// within 24 hours returns the original response without re-processing.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new
            {
                type = "https://api.myapp.com/errors/missing-idempotency-key",
                title = "Idempotency-Key Required",
                status = 400,
                detail = "Include a unique Idempotency-Key header to safely retry this request."
            });

        var order = await _orders.CreateV3Async(request, idempotencyKey, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToV3Dto(order));
    }

    /// <summary>Cancels an order. Returns 409 if the order is already shipped.</summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound();

        if (order.Status is Domain.OrderStatus.Shipped or Domain.OrderStatus.Delivered)
            return Conflict(new
            {
                type = "https://api.myapp.com/errors/cannot-cancel-shipped-order",
                title = "Conflict",
                status = 409,
                detail = $"Order '{id}' has already been {order.Status.ToString().ToLower()} and cannot be cancelled."
            });

        var cancelled = await _orders.CancelAsync(id, ct);
        return Ok(MapToV3Dto(await _orders.GetByIdAsync(id, ct)!));
    }

    /// <summary>Initiates a full or partial refund on a delivered order.</summary>
    [HttpPost("{id}/refund")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Refund(
        string id,
        [FromBody] RefundRequest request,
        CancellationToken ct)
    {
        var order = await _orders.RefundAsync(id, request.Amount, ct);
        return Ok(MapToV3Dto(order));
    }

    // ── Private mapper ────────────────────────────────────────────────────────
    private OrderDto MapToV3Dto(Order o)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v3/orders";

        return new OrderDto(
            o.Id,
            o.CustomerId,
            new MoneyDto(o.Total, "USD"),
            new MoneyDto(o.TaxAmount, "USD"),
            new MoneyDto(o.ShippingAmount, "USD"),
            Enum.Parse<OrderStatus>(o.Status.ToString()),
            o.PaymentMethod ?? "unknown",
            o.ShippingAddress is null
                ? new AddressDto("", "", "", "")
                : new AddressDto(
                    o.ShippingAddress.Street, o.ShippingAddress.City,
                    o.ShippingAddress.PostalCode, o.ShippingAddress.Country),
            o.BillingAddress is null ? null
                : new AddressDto(
                    o.BillingAddress.Street, o.BillingAddress.City,
                    o.BillingAddress.PostalCode, o.BillingAddress.Country),
            o.Items.Select(i => new LineItemDto(
                i.ProductId, i.ProductName, i.Sku, i.Quantity,
                new MoneyDto(i.UnitPrice, "USD"),
                new MoneyDto(i.Discount ?? 0, "USD"),
                new MoneyDto(i.LineTotal, "USD"))).ToList(),
            o.Events.Select(e => new OrderEventDto(
                e.EventType, e.Description, e.OccurredAt)).ToList(),
            new OrderLinksDto(
                Self: $"{baseUrl}/{o.Id}",
                Cancel: o.Status is Domain.OrderStatus.Pending or Domain.OrderStatus.PaymentConfirmed
                    ? $"{baseUrl}/{o.Id}/cancel" : null,
                Refund: o.Status == Domain.OrderStatus.Delivered
                    ? $"{baseUrl}/{o.Id}/refund" : null,
                Track: o.TrackingUrl),
            o.CreatedAt,
            o.UpdatedAt,
            o.FulfilledAt);
    }
}

public sealed record RefundRequest(decimal? Amount = null);  // null = full refund