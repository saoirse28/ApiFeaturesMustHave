namespace Resilience.DTOs
{
    public class ChargeRequest
    {
        public string IdempotencyKey { get; set; }
        public decimal Amount { get; set; }
        public string OrderId { get; set; }
        public string Currency { get; set; }
    }
}
