namespace ServerSentEvents.Services
{
    public class OrderService : IOrderService
    {
        public Task<string> GetByIdAsync(string orderId, CancellationToken ct)
        {
            return Task.FromResult($"Order {orderId} details");
        }
    }
}
