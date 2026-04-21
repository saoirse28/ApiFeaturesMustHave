namespace Resilience.DTOs
{
    public class Order
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public decimal Total { get; set; }

        public static Order Create(CreateOrderRequest request)
        {
            return new Order
            {
                ProductId = request.ProductId
            };
        }

        internal static Order Create(Models.CreateOrderRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
