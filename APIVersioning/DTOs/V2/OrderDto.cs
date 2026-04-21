namespace APIVersioning.DTOs.V2;

/// <summary>
/// V2 order representation — extended model.
/// Breaking changes from V1:
///   - Added ShippingAddress (new required concept)
///   - Added PaymentMethod
///   - Renamed "Total" → "TotalAmount" with currency
///   - Added LineItems with discounts
///   - Pagination uses offset-based metadata envelope
/// </summary>
public sealed record OrderDto(
    string Id,
    string CustomerId,
    MoneyDto TotalAmount,
    string Status,
    string PaymentMethod,
    AddressDto ShippingAddress,
    List<LineItemDto> LineItems,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record MoneyDto(decimal Amount, string Currency);

public sealed record AddressDto(
    string Street,
    string City,
    string PostalCode,
    string Country
);

public sealed record LineItemDto(
    string ProductId,
    string ProductName,
    int Quantity,
    MoneyDto UnitPrice,
    MoneyDto? Discount,
    MoneyDto LineTotal
);

public sealed record CreateOrderRequest(
    string CustomerId,
    AddressDto ShippingAddress,
    string PaymentMethod,
    List<OrderItemDto> Items
);

public sealed record OrderItemDto(
    string ProductId,
    int Quantity
);

public sealed record PagedOrderResponse(
    List<OrderDto> Data,
    PaginationMeta Meta
);

public sealed record PaginationMeta(
    int Total,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);