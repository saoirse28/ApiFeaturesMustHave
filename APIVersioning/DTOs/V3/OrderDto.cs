namespace APIVersioning.DTOs.V3;

/// <summary>
/// V3 order representation — current recommended shape.
/// Breaking changes from V2:
///   - Cursor-based pagination replaces offset pagination
///   - Errors follow RFC 7807 ProblemDetails
///   - Webhook events model added
///   - Order state machine exposed via _links (HATEOAS-lite)
///   - Idempotency key required on Create
/// </summary>
public sealed record OrderDto(
    string Id,
    string CustomerId,
    MoneyDto TotalAmount,
    MoneyDto TaxAmount,
    MoneyDto ShippingAmount,
    OrderStatus Status,
    string PaymentMethod,
    AddressDto ShippingAddress,
    AddressDto? BillingAddress,
    List<LineItemDto> LineItems,
    List<OrderEventDto> Events,
    OrderLinksDto Links,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? FulfilledAt
);

public enum OrderStatus
{
    Pending, PaymentConfirmed, Processing,
    Shipped, Delivered, Cancelled, Refunded
}

public sealed record MoneyDto(decimal Amount, string Currency);

public sealed record AddressDto(
    string Street,
    string City,
    string PostalCode,
    string CountryCode,
    string? State = null
);

public sealed record LineItemDto(
    string ProductId,
    string ProductName,
    string? Sku,
    int Quantity,
    MoneyDto UnitPrice,
    MoneyDto? Discount,
    MoneyDto LineTotal
);

public sealed record OrderEventDto(
    string EventType,
    string Description,
    DateTimeOffset OccurredAt
);

/// <summary>HATEOAS-lite: exposes valid state transitions as links.</summary>
public sealed record OrderLinksDto(
    string Self,
    string? Cancel,
    string? Refund,
    string? Track
);

public sealed record CreateOrderRequest(
    string CustomerId,
    string IdempotencyKey,         // required in V3
    AddressDto ShippingAddress,
    AddressDto? BillingAddress,
    string PaymentMethodId,
    string? CouponCode,
    List<OrderItemDto> Items
);

public sealed record OrderItemDto(
    string ProductId,
    int Quantity,
    string? Note = null
);

/// <summary>Cursor-based pagination envelope.</summary>
public sealed record CursorPagedResponse<T>(
    List<T> Data,
    CursorMeta Meta
);

public sealed record CursorMeta(
    string? NextCursor,
    string? PreviousCursor,
    int PageSize,
    bool HasMore
);