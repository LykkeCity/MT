using JetBrains.Annotations;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public interface IAccountGroupCacheService
    {
        IAccountGroup[] GetAllAccountGroups();
        [CanBeNull] IAccountGroup GetAccountGroup(string tradingConditionId, string accountAssetId);
    }
}
