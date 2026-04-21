using APIVersioning.DTOs.V1;
using APIVersioning.Services;
using APIVersioning.Versioning;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using APIVersioning.Models;

namespace APIVersioning.Controllers.V1;

/// <summary>
/// V1 Orders API — DEPRECATED.
/// Sunset date: 2025-07-01. Please migrate to V3.
/// </summary>
[ApiController]
[ApiVersion(ApiVersions.V1)]
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

    /// <summary>Returns a paginated list of orders for the authenticated customer.</summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (max 100).</param>
    [HttpGet]
    [ProducesResponseType<OrderListResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (orders, total) = await _orders.GetPagedAsync(page, pageSize, ct);

        var dtos = orders.Select(o => new OrderDto(
            o.Id, o.CustomerId, o.Total, o.Status.ToString(), o.CreatedAt))
            .ToList();

        return Ok(new OrderListResponse(dtos, total, page, pageSize));
    }

    /// <summary>Returns a single order by ID.</summary>
    /// <param name="id">Order identifier.</param>
    [HttpGet("{id}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound();

        return Ok(new OrderDto(
            order.Id, order.CustomerId, order.Total,
            order.Status.ToString(), order.CreatedAt));
    }

    /// <summary>Creates a new order.</summary>
    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var order = await _orders.CreateAsync(request.CustomerId,
            request.Items.Select(i => (i.ProductId, i.Quantity, i.UnitPrice)).ToList(),
            ct);

        var dto = new OrderDto(
            order.Id, order.CustomerId, order.Total,
            order.Status.ToString(), order.CreatedAt);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, dto);
    }
}