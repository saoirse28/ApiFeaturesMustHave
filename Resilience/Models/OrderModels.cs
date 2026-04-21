namespace Resilience.Models;

public sealed record CreateOrderRequest(
    string CustomerId,
    string Region,
    string Channel,
    string PaymentMethodId,
    List<OrderItemDto> Items,
    string ProductId
)
{
    public object UserId { get; internal set; }
}

public sealed record UpdateOrderRequest(
    string? ShippingAddress,
    List<OrderItemDto>? Items
);

public sealed record OrderItemDto(
    string ProductId,
    int Quantity
);

public sealed record OrderDto(
    string Id,
    string CustomerId,
    decimal Total,
    string Status,
    string PaymentMethod,
    DateTimeOffset CreatedAt,
    List<OrderItemDto>? Items
);

public sealed record PagedOrderResponse(
    List<OrderDto> Orders,
    int Total,
    int Page,
    int PageSize
);

public sealed record CreateOrderResult(
    bool Succeeded,
    OrderDto? Order = null,
    string? ErrorMessage = null
)
{
    public static CreateOrderResult Success(DTOs.Order order1, OrderDto order) => new(true, order);
    public static CreateOrderResult Failure(string msg) => new(false, null, msg);

    internal static CreateOrderResult Success(DTOs.Order order, string transactionId)
    {
        throw new NotImplementedException();
    }
}

public sealed record StockCheckResult(
    string OrderId,
    List<ItemStockLevel> Items,
    bool IsDegraded,
    DateTimeOffset CheckedAt
);

public sealed record ItemStockLevel(
    string ProductId,
    bool Available,
    int Quantity,
    bool IsEstimated
);

public sealed record CircuitStatusResponse(
    string Payment,
    string Inventory,
    string Database,
    string Redis,
    DateTimeOffset CheckedAt
);