using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingOrderActionRepository
    {
        Task AddAsync(IOrderAction order);

        Task<IEnumerable<IOrderAction>> GetOrdersAsync(DateTime? from = null, DateTime? to = null);

        bool Any();
    }
}
