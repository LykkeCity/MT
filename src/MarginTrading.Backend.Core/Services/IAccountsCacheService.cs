using System;
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
        PaginatedResponse<MarginTradingAccount> GetAllByPages(int? skip = null, int? take = null);
        IEnumerable<string> GetClientIdsByTradingConditionId(string tradingConditionId, string accountId = null);

        void TryAddNew(MarginTradingAccount account);
        
        void Update(MarginTradingAccount newValue);

        void UpdateAccountChanges(string accountId, string updatedTradingConditionId,
            decimal updatedWithdrawTransferLimit, bool isDisabled, bool isWithdrawalDisabled);
        void UpdateAccountBalance(string accountId, decimal accountBalance);

        Task FreezeWithdrawalMargin(string operationId, string clientId, string accountId, decimal amount);
        Task UnfreezeWithdrawalMargin(string operationId);

        bool CheckEventTimeNewer(string accountId, DateTime eventTime);
    }
}