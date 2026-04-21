namespace ProperLogging.DTOs
{
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public string TransactionId { get; set; }
    }
}
