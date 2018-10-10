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

        void TryAddNew(MarginTradingAccount account);

        Task<bool> UpdateAccountChanges(string accountId, string updatedTradingConditionId,
            decimal updatedWithdrawTransferLimit, bool isDisabled, bool isWithdrawalDisabled, DateTime eventTime);
        Task<bool> UpdateAccountBalance(string accountId, decimal accountBalance, DateTime eventTime);
        
        bool TryStartLiquidation(string accountId, string operationId, out string currentOperationId);
        
        bool TryFinishLiquidation(string accountId, string reason, string liquidationOperationId = null);
    }
}