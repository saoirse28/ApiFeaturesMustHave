using ExceptionHandling.DTOs;
using ExceptionHandling.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExceptionHandling.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
        => _orderService = orderService;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        // Throws NotFoundException if not found — caught by DomainExceptionHandler
        var order = await _orderService.GetByIdAsync(id, ct);
        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        // Throws ValidationException — caught by ValidationExceptionHandler
        // Throws ConflictException   — caught by DomainExceptionHandler
        var order = await _orderService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        // Throws NotFoundException  — caught by DomainExceptionHandler
        // Throws ForbiddenException — caught by DomainExceptionHandler
        await _orderService.DeleteAsync(id, ct);
        return NoContent();
    }
}
