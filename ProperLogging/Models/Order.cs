using ProperLogging.DTOs;

namespace ProperLogging.Models
{
    public class Order
    {
        public Order (CreateOrderRequest request)
        {
            Id = request.Id;
            Reference = request.Reference;
            OwnerId = request.OwnerId;
        }
        public string Id { get; init; } = "001";
        public string OwnerId { get; set; } = "CUSTOMER";
        public string Reference { get; set; } = "";

        public decimal Total { get; set; }
    }
}
