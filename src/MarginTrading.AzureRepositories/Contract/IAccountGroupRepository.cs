using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.AzureRepositories.Contract
{
    public interface IAccountGroupRepository
    {
        Task AddOrReplaceAsync(IAccountGroup group);
        Task<IAccountGroup> GetAsync(string tradingConditionId, string baseAssetId);
        Task<IEnumerable<IAccountGroup>> GetAllAsync();
    }
}
