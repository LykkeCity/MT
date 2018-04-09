using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.TradingConditions;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IAccountGroupsReadingApi
    {
        /// <summary>
        /// Returns all account groups
        /// </summary>
        [Get("/api/accountGroups/")]
        Task<List<AccountGroupContract>> List();

        /// <summary>
        /// Returns an account group by tradingConditionId and baseAssetId
        /// </summary>
        [Get("/api/accountGroups/byBaseAsset/{tradingConditionId}/{baseAssetId}")]
        Task<AccountGroupContract> GetByBaseAsset(string tradingConditionId, string baseAssetId);
    }
}
