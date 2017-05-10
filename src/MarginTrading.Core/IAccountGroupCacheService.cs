namespace MarginTrading.Core
{
    public interface IAccountGroupCacheService
    {
        IMarginTradingAccountGroup[] GetAllAccountGroups();
        IMarginTradingAccountGroup GetAccountGroup(string tradingConditionId, string accountAssetId);
    }
}
