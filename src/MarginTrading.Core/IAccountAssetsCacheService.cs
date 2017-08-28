using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IAccountAssetsCacheService
    {
        IMarginTradingAccountAsset GetAccountAsset(string tradingConditionId, string accountAssetId, string instrument);
        IMarginTradingAccountAsset GetAccountAssetThrowIfNotFound(string tradingConditionId, string accountAssetId, string instrument);
        Dictionary<string, IMarginTradingAccountAsset[]> GetClientAssets(IEnumerable<MarginTradingAccount> accounts);
        List<string> GetAccountAssetIds(string tradingConditionId, string baseAssetId);
        List<IMarginTradingAccountAsset> GetAccountAssets(string tradingConditionId, string baseAssetId);
        bool IsInstrumentSupported(string instrument);
    }
}
