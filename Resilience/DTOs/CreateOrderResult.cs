using System.Reflection.Metadata.Ecma335;

namespace Resilience.DTOs
{
    public class CreateOrderResult
    {
        public string Status { get; set; }

        public static CreateOrderResult Failure(string v)
        {
            return new CreateOrderResult { Status = "Failure" ?? string.Empty };
        }
        public static CreateOrderResult Success(Order order, string TransactionId)
        {
            return new CreateOrderResult { Status = "Success" ?? string.Empty };
        }
    }
}
