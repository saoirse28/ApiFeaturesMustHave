using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RateLimiting.DTOs;
using RateLimiting.RateLimiting;
using RateLimiting.Services;

namespace RateLimiting.Controllers;

[ApiController]
[Route("api/v1/orders")]
[EnableRateLimiting(RateLimitPolicies.PerClient)]   // default for all actions in this controller
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    // ── Standard CRUD — inherits controller-level PerClient policy ────────────

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken ct)
        => Ok(await _orders.GetListAsync(ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    // ── POST — uses the tiered policy (premium gets more quota) ───────────────

    [HttpPost]
    [EnableRateLimiting(TieredRateLimitPolicy.PolicyName)]   // overrides controller policy
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var order = await _orders.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // ── Bulk endpoint — stricter limit than standard ───────────────────────────

    [HttpPost("bulk")]
    [EnableRateLimiting(RateLimitPolicies.TrustedPartner)]   // only partners can bulk-create
    public async Task<IActionResult> BulkCreate(
        [FromBody] IEnumerable<CreateOrderRequest> requests,
        CancellationToken ct)
    {
        var orders = await _orders.BulkCreateAsync(requests, ct);
        return Ok(orders);
    }

    // ── Export — no rate limiting for internal service-to-service calls ────────

    [HttpGet("export")]
    [DisableRateLimiting]   // explicitly opt out — internal only, protected by mTLS
    public async Task<IActionResult> Export(CancellationToken ct)
        => Ok(await _orders.ExportAllAsync(ct));
}