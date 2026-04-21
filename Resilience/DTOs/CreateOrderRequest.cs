using StackExchange.Redis;

namespace Resilience.DTOs
{
    public class CreateOrderRequest
    {
        public string ProductId { get; set; }
        public string UserId { get; set; }
    }
}
