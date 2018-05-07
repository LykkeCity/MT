using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    public interface IOrdersHistoryRepository
    {
        Task AddAsync(IOrderHistory order);
        Task<IEnumerable<IOrderHistory>> GetHistoryAsync();
        Task<IReadOnlyList<IOrderHistory>> GetHistoryAsync(string[] accountIds, DateTime? from, DateTime? to);
    }
}
