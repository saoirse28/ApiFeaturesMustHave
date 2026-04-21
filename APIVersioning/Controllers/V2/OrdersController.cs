using APIVersioning.DTOs.V2;
using APIVersioning.Models;
using APIVersioning.Services;
using APIVersioning.Versioning;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MoneyDto = APIVersioning.DTOs.V2.MoneyDto;

namespace APIVersioning.Controllers.V2;

/// <summary>
/// V2 Orders API — STABLE.
/// Breaking changes from V1: extended order model, offset pagination envelope,
/// shipping address required, payment method added.
/// </summary>
[ApiController]
[ApiVersion(ApiVersions.V2)]
[Route("api/v{version:apiVersion}/orders")]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>Returns a paginated list of orders with full order details.</summary>
    [HttpGet]
    [ProducesResponseType<PagedOrderResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (orders, total) = await _orders.GetPagedAsync(page, pageSize, ct);

        var dtos = orders.Select(MapToV2Dto).ToList();

        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        return Ok(new PagedOrderResponse(
            dtos,
            new PaginationMeta(
                total, page, pageSize, totalPages,
                HasNextPage: page < totalPages,
                HasPreviousPage: page > 1)));
    }

    /// <summary>Returns a single order by ID with full V2 details.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound();
        return Ok(MapToV2Dto(order));
    }

    /// <summary>Creates a new order. Requires shipping address and payment method.</summary>
    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var order = await _orders.CreateV2Async(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToV2Dto(order));
    }

    /// <summary>Cancels an order if it has not yet been shipped.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        var cancelled = await _orders.CancelAsync(id, ct);
        return cancelled ? NoContent() : Conflict(new { error = "Order cannot be cancelled" });
    }

    // ── Private mapper ────────────────────────────────────────────────────────
    private static OrderDto MapToV2Dto(Order o) => new(
        o.Id,
        o.CustomerId,
        new MoneyDto(o.Total, "USD"),
        o.Status.ToString(),
        o.PaymentMethod ?? "unknown",
        o.ShippingAddress is null
            ? new AddressDto("", "", "", "")
            : new AddressDto(
                o.ShippingAddress.Street,
                o.ShippingAddress.City,
                o.ShippingAddress.PostalCode,
                o.ShippingAddress.Country),
        [.. o.Items.Select(i => new LineItemDto(
            i.ProductId, i.ProductName, i.Quantity,
            new MoneyDto(i.UnitPrice, "USD"),
            new MoneyDto(i.Discount ?? 0, "USD"),
            new MoneyDto(i.LineTotal, "USD")))],
        o.CreatedAt,
        o.UpdatedAt);
}