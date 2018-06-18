using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IAccountMarginFreezingRepository
    {
        Task<IReadOnlyList<IAccountMarginFreezing>> GetAllAsync();
        Task<bool> TryInsertAsync(IAccountMarginFreezing item);
        Task DeleteAsync(string operationId);
    }
}