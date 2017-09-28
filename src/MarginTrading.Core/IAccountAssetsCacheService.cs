using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IAccountAssetsCacheService
    {
        IAccountAssetPair GetAccountAsset(string tradingConditionId, string accountAssetId, string instrument);
        IAccountAssetPair GetAccountAssetThrowIfNotFound(string tradingConditionId, string accountAssetId, string instrument);
        Dictionary<string, IAccountAssetPair[]> GetClientAssets(IEnumerable<MarginTradingAccount> accounts);
        List<string> GetAccountAssetIds(string tradingConditionId, string baseAssetId);
        List<IAccountAssetPair> GetAccountAssets(string tradingConditionId, string baseAssetId);
        bool IsInstrumentSupported(string instrument);
    }
}
