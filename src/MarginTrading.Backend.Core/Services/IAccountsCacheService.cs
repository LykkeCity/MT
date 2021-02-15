// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
        void Remove(string accountId);
        string Reset(string accountId, DateTime eventTime);

        Task<bool> UpdateAccountChanges(string accountId, string updatedTradingConditionId,
            decimal updatedWithdrawTransferLimit, bool isDisabled, bool isWithdrawalDisabled, DateTime eventTime, string additionalInfo);
        Task<bool> UpdateAccountBalance(string accountId, decimal accountBalance, DateTime eventTime);
        
        bool TryStartLiquidation(string accountId, string operationId, out string currentOperationId);
        
        bool TryFinishLiquidation(string accountId, string reason, string liquidationOperationId = null);
    }
}