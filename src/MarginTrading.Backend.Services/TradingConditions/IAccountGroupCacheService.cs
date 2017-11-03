using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public interface IAccountGroupCacheService
    {
        IAccountGroup[] GetAllAccountGroups();
        IAccountGroup GetAccountGroup(string tradingConditionId, string accountAssetId);
    }
}
