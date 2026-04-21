namespace ProperLogging.DTOs
{
    public record CreateOrderRequest
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string Reference { get; set; }
        public string OwnerId {  get; set; }
    }
}
