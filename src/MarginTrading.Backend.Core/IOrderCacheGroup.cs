using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace MarginTrading.Backend.Core
{
    public interface IOrderCacheGroup
    {
        OrderStatus Status { get; }
        
        IOrderCacheGroup Init(IReadOnlyCollection<Order> orders);

        Task AddAsync(Order order);
        Task RemoveAsync(Order order);
        Order GetOrderById(string orderId);
        bool TryGetOrderById(string orderId, out Order result);
        IReadOnlyCollection<Order> GetOrdersByInstrument(string instrument);
        IReadOnlyCollection<Order> GetOrdersByMarginInstrument(string instrument);
        ICollection<Order> GetOrdersByInstrumentAndAccount(string instrument, string accountId);
        IReadOnlyCollection<Order> GetAllOrders();
        ICollection<Order> GetOrdersByAccountIds(params string[] accountIds);
        
    }
}