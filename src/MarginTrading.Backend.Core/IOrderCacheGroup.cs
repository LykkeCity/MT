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
        Task<Order> GetOrderById(string orderId);
        Task<Order> GetOrderByIdOrDefault(string orderId);
        Task<IReadOnlyCollection<Order>> GetOrdersByInstrument(string instrument);
        Task<IReadOnlyCollection<Order>> GetOrdersByMarginInstrument(string instrument);
        Task<ICollection<Order>> GetOrdersByInstrumentAndAccount(string instrument, string accountId);
        Task<IReadOnlyCollection<Order>> GetAllOrders();
        Task<ICollection<Order>> GetOrdersByAccountIds(params string[] accountIds);
        
    }
}