using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IPositionsHistoryRepository
    {
        Task<IReadOnlyList<IPositionHistory>> GetLastSnapshot(DateTime @from);
    }
}
