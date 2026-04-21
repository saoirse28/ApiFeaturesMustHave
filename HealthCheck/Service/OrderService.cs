using HealthCheckMetric.Metrics;

namespace HealthCheckMetric.Service
{
    // Usage inside a service or controller
    public class OrderService
    {
        private readonly AppMetrics _metrics;

        public OrderService(AppMetrics metrics) => _metrics = metrics;

        public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var order = new Order(request);
            await OrderRepository.SaveAsync(order);

            sw.Stop();

            // Record metrics with dimensional tags
            _metrics.OrdersCreated.Add(1,
                new KeyValuePair<string, object?>("region", request.Region),
                new KeyValuePair<string, object?>("channel", request.Channel));

            _metrics.OrderProcessingDuration.Record(
                sw.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("region", request.Region));

            _metrics.OrderValueAmount.Record(
                order.TotalCents,
                new KeyValuePair<string, object?>("currency", order.Currency));

            return order;
        }
    }

    public class OrderRepository
    {
        public static async Task<bool> SaveAsync(Order order)
        {
            await Task.Delay(100);
            return true;
        }
    }

    public class Order
    {
        public long TotalCents { get; set; }
        public string Currency { get; set; }

        public Order(CreateOrderRequest request)
        {
            // Initialize order properties based on the request
            TotalCents = request.TotalCents;
            Currency = request.Currency;
        }
    }
    public class CreateOrderRequest
    {
        public long TotalCents { get; set; }
        public string Currency { get; set; }
        public string Region { get; set; }
        public string Channel { get; set; }
    }
}