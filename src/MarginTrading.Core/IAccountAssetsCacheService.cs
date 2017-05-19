using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IAccountAssetsCacheService
    {
        IMarginTradingAccountAsset GetAccountAsset(string tradingConditionId, string accountAssetId, string instrument);
        IMarginTradingAccountAsset GetAccountAssetNoThrowExceptionOnInvalidData(string tradingConditionId, string accountAssetId, string instrument);
        Dictionary<string, IMarginTradingAccountAsset[]> GetClientAssets(IEnumerable<MarginTradingAccount> accounts);
        List<string> GetAccountAssetIds(string tradingConditionId, string accountAssetId);
        List<IMarginTradingAccountAsset> GetAccountAssets(string tradingConditionId, string accountAssetId);
    }
}
