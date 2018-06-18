using System.Collections.Generic;
using System.Threading.Tasks;
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

        Task FreezeWithdrawalMargin(string operationId, string clientId, string accountId, decimal amount);
        Task UnfreezeWithdrawalMargin(string operationId);
    }
}