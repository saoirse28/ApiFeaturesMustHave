using StackExchange.Redis;

namespace ServerSentEvents.Services
{
    public interface IOrderService
    {
        public Task<string> GetByIdAsync(string orderId, CancellationToken ct);
    }
}
