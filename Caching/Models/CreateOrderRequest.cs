using ExceptionHandling.Models;

namespace Caching.Models
{
    public class CreateOrderRequest
    {
        public string Id { get; init; } = "001";
        public string OwnerId { get; set; } = "CUSTOMER";
        public string Reference { get; set; } = "";
    }
}
