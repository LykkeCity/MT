using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IAccountsCacheService
    {
        [NotNull]
        MarginTradingAccount Get(string accountId);

        [CanBeNull]
        MarginTradingAccount TryGet(string accountId);

        IReadOnlyList<MarginTradingAccount> GetAll();
        IEnumerable<string> GetClientIdsByTradingConditionId(string tradingConditionId, string accountId = null);
        void Update(MarginTradingAccount newValue);
    }
}