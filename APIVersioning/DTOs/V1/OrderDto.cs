namespace APIVersioning.DTOs.V1
{
    /// <summary>
    /// V1 order representation — simple flat model.
    /// This shape is frozen. Never add new required fields here — that
    /// would break existing V1 clients.
    /// </summary>
    public sealed record OrderDto(
        string Id,
        string CustomerId,
        decimal Total,
        string Status,
        DateTime CreatedAt
    );

    public sealed record CreateOrderRequest(
        string CustomerId,
        List<OrderItemDto> Items
    );

    public sealed record OrderItemDto(
        string ProductId,
        int Quantity,
        decimal UnitPrice
    );

    public sealed record OrderListResponse(
        List<OrderDto> Orders,
        int Total,
        int Page,
        int PageSize
    );

}
