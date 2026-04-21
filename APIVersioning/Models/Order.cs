using APIVersioning.Domain;

namespace APIVersioning.Models
{
    public class Order
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public int Total { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PaymentMethod { get; set; }
        public Address ShippingAddress { get; set; }
        public List<Items> Items { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public Address BillingAddress { get; set; }
        public string TrackingUrl { get; set; }
        public DateTimeOffset FulfilledAt { get; set; }
        public IEnumerable<OrderEvent> Events { get; internal set; }
    }

    public sealed record OrderEvent(
    string EventType,
    string Description,
    DateTimeOffset OccurredAt
);

    public sealed record Address(
    string Street,
    string City,
    string PostalCode,
    string Country
    );
    public sealed record Items(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal? Discount,
    decimal LineTotal,
    string Sku
    );
}
