using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.TradingConditions;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface ITradingConditionsReadingApi
    {
        /// <summary>
        /// Get all trading conditions
        /// </summary>
        /// <returns></returns>
        [Get("/api/tradingConditions/")]
        Task<List<TradingConditionContract>> List();
        
        /// <summary>
        /// Get trading condition by Id
        /// </summary>
        /// <returns></returns>
        [Get("/api/tradingConditions/{id}")]
        Task<TradingConditionContract> Get(string id);
    }
}
