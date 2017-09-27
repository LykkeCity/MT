using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingOrdersHistoryRepository
    {
        Task AddAsync(IOrderHistory order);
        Task<IEnumerable<IOrderHistory>> GetHistoryAsync();
        Task<IReadOnlyList<IOrderHistory>> GetHistoryAsync(string clientId, string[] accountIds, DateTime? from, DateTime? to);
    }
}
