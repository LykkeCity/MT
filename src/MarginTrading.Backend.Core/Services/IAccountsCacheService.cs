using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IAccountsCacheService
    {
        IEnumerable<MarginTradingAccount> GetAll([CanBeNull] string clientId);
        IReadOnlyList<MarginTradingAccount> GetAll();
        MarginTradingAccount Get(string clientId, string accountId);
        void UpdateBalance(MarginTradingAccount account);
        IMarginTradingAccount SetTradingCondition(string clientId, string accountId, string tradingConditionId);
        IEnumerable<string> GetClientIdsByTradingConditionId(string tradingConditionId, string accountId = null);
        void UpdateAccountsCache(string clientId, IEnumerable<MarginTradingAccount> newValues);
        MarginTradingAccount TryGet(string clientId, string accountId);
    }
}
