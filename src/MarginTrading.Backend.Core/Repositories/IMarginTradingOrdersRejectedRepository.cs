using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public interface IMarginTradingOrdersRejectedRepository
    {
        Task AddAsync(IOrderHistory order);
        Task<IEnumerable<IOrderHistory>> GetHisotryAsync(string[] accountIds, DateTime from, DateTime to);
    }
}
