using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;

namespace MarginTrading.DataReader.Services
{
    public interface IOrdersSnapshotReaderService
    {
        Task<IReadOnlyList<Order>> GetAllAsync();
        Task<IReadOnlyList<Order>> GetActiveAsync();
        Task<IReadOnlyList<Order>> GetPendingAsync();
        Task<IReadOnlyList<Order>> GetActiveByAccountIdsAsync(string[] accountIds);
    }
}
