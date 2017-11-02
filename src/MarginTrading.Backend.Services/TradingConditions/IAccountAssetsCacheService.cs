using System.Collections.Generic;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public interface IAccountAssetsCacheService
    {
        IAccountAssetPair GetAccountAsset(string tradingConditionId, string accountAssetId, string instrument);
        IAccountAssetPair GetAccountAssetThrowIfNotFound(string tradingConditionId, string accountAssetId, string instrument);
        Dictionary<string, IAccountAssetPair[]> GetClientAssets(IEnumerable<MarginTradingAccount> accounts);
        ICollection<IAccountAssetPair> GetAccountAssets(string tradingConditionId, string accountAssetId);
    }
}
