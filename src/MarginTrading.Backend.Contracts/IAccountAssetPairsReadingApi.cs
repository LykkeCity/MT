using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.AccountAssetPair;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IAccountAssetPairsReadingApi
    {
        /// <summary>
        /// Get all asset pairs
        /// </summary>
        [Get("/api/accountAssetPairs/")]
        Task<List<AccountAssetPairContract>> List();

        /// <summary>
        /// Get asset by tradingcondition and base asset
        /// </summary>
        [Get("/api/accountAssetPairs/byAsset/{tradingConditionId}/{baseAssetId}")]
        Task<List<AccountAssetPairContract>> Get(string tradingConditionId, string baseAssetId);

        /// <summary>
        /// 
        /// </summary>
        [Get("/api/accountAssetPairs/byAssetPair/{tradingConditionId}/{baseAssetId}/{assetPairId}")]
        Task<AccountAssetPairContract> Get(string tradingConditionId, string baseAssetId, string assetPairId);
    }
}