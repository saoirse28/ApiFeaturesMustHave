using ExceptionHandling.DTOs;
using ExceptionHandling.Models;

namespace ExceptionHandling.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly List<Order> _orderList = new List<Order>();

        public OrderRepository() 
        {
            var totalOrders = 100; // Simulate total Order in the database
            for (int i = 0; i < totalOrders; i++)
            {
                int Id = i + 1;

                _orderList.Add(new Order(new CreateOrderRequest
                {
                    Id = $"{Id}",
                    CustomerId = $"CustomerId {Id}",
                    Reference = $"Reference {Id}",
                    Items =
                    [
                        new Item(Id.ToString(), $"Name{Id}", Id * 10, Id)
                    ],
                    OwnerId = "CUSTOMER"
                }));
            }
        }
        public Task DeleteAsync(string id, CancellationToken ct)
        {
            var order = _orderList.FirstOrDefault(x => x.Id == id);
            _orderList.Remove(order);
            return Task.Delay(1000, ct);
        }

        public Task<Order> FindByIdAsync(string id, CancellationToken ct)
        {
            var order = _orderList.FirstOrDefault(x => x.Id == id);
            return Task.FromResult<Order>(order);
        }

        public Task<Order> FindByReferenceAsync(string reference, CancellationToken ct)
        {
            var order = _orderList.FirstOrDefault(x => x.Reference == reference);
            return Task.FromResult<Order>(order);
        }

        public Task SaveAsync(Order order, CancellationToken ct)
        {
            _orderList.Add(order);
            return Task.Delay(1000, ct);
        }
    }
}
