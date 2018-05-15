using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IAccountsCacheService
    {
        IReadOnlyList<MarginTradingAccount> GetAll();
        MarginTradingAccount Get(string accountId);
        void UpdateBalance(MarginTradingAccount account);
        IEnumerable<string> GetClientIdsByTradingConditionId(string tradingConditionId, string accountId = null);
        void Update(MarginTradingAccount newValue);
        MarginTradingAccount TryGet(string accountId);
    }
}
