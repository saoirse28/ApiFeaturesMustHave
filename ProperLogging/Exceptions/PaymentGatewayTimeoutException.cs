namespace ProperLogging.Exceptions
{
    public class PaymentGatewayTimeoutException : Exception
    {
        public string Gateway { get; set; }

    }
}
