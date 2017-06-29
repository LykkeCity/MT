using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IAccountsCacheService
    {
        IEnumerable<MarginTradingAccount> GetAll(string clientId = null);
        MarginTradingAccount Get(string clientId, string accountId);
        void UpdateBalance(MarginTradingAccount account);
        IMarginTradingAccount SetTradingCondition(string clientId, string accountId, string tradingConditionId);
        IEnumerable<string> GetClientIdsByTradingConditionId(string tradingConditionId, string accountId = null);
        void UpdateAccountsCache(string clientId, IEnumerable<MarginTradingAccount> newValues);
        void AddAccount(MarginTradingAccount account);
    }
}
