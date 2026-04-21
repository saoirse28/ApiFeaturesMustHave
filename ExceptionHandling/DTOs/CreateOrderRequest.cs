using ExceptionHandling.Models;

namespace ExceptionHandling.DTOs
{
    public record CreateOrderRequest
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public List<Item> Items { get; set; }
        public string Reference { get; set; }
        public string OwnerId {  get; set; }
    }
}
