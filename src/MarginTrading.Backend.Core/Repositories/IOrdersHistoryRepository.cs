using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public interface IOrdersHistoryRepository
    {
        Task AddAsync(IOrderHistory order);
        Task<IEnumerable<IOrderHistory>> GetHistoryAsync();
        Task<IReadOnlyList<IOrderHistory>> GetHistoryAsync(string[] accountIds, DateTime? from, DateTime? to);
    }
}
