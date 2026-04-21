using ExceptionHandling.DTOs;
using static ExceptionHandling.Services.OrderService;

namespace ExceptionHandling.Models
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
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string OwnerId { get; set; } = "CUSTOMER";
        public string Reference { get; set; } = "";
    }
}
