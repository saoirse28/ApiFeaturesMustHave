using ExceptionHandling.Models;

namespace Caching.Models
{
    public class Order(CreateOrderRequest request)
    {
        public string Id { get; init; } = request.Id;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string OwnerId { get; set; } = request.OwnerId;
        public string Reference { get; set; } = request.Reference;
    }
}
