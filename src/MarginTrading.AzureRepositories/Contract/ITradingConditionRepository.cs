using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.AzureRepositories.Contract
{
    public interface ITradingConditionRepository
    {
        Task AddOrReplaceAsync(ITradingCondition condition);
        Task<ITradingCondition> GetAsync(string tradingConditionId);
        Task<IEnumerable<ITradingCondition>> GetAllAsync();
    }
}
