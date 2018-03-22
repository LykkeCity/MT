using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.AccountAssetPair;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.TradingConditions;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface ITradingConditionsEditingApi
    {
        /// <summary>
        /// Insert or update trading condition
        /// </summary>
        /// <param name="tradingCondition"></param>
        /// <returns></returns>
        [Post("/api/tradingConditions")]
        Task<BackendResponse<TradingConditionContract>> InsertOrUpdate([Body] TradingConditionContract tradingCondition);

        /// <summary>
        /// Insert or update account group
        /// </summary>
        /// <param name="accountGroup"></param>
        /// <returns></returns>
        [Post("/api/tradingConditions/accountGroups")]
        Task<BackendResponse<AccountGroupContract>> InsertOrUpdateAccountGroup([Body] AccountGroupContract accountGroup);

        /// <summary>
        /// Assing instruments
        /// </summary>
        /// <param name="assignInstruments"></param>
        /// <returns></returns>
        [Post("/api/tradingConditions/accountAssets/assignInstruments")]
        Task<BackendResponse<List<AccountAssetPairContract>>> AssignInstruments([Body] AssignInstrumentsContract assignInstruments);

        /// <summary>
        /// Insert or update account asset pair
        /// </summary>
        /// <param name="accountAsset"></param>
        /// <returns></returns>
        [Post("/api/tradingConditions/accountAssets")]
        Task<BackendResponse<AccountAssetPairContract>> InsertOrUpdateAccountAsset([Body] AccountAssetPairContract accountAsset);
    }
}
