// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Extensions;

namespace MarginTrading.Backend.Services.Services
{
    /// <summary>
    /// Provides accounts depending on request scope: either from current system state or from trading snapshot draft
    /// </summary>
    public class AccountsProvider : IAccountsProvider
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IDraftSnapshotKeeper _draftSnapshotKeeper;
        private readonly ILog _log;

        public AccountsProvider(IAccountsCacheService accountsCacheService, IDraftSnapshotKeeper draftSnapshotKeeper, ILog log)
        {
            _accountsCacheService = accountsCacheService;
            _draftSnapshotKeeper = draftSnapshotKeeper;
            _log = log;
        }

        public MarginTradingAccount GetAccountById(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentNullException(nameof(accountId));
            
            if (_draftSnapshotKeeper.Initialized())
            {
                _log.WriteInfoAsync(nameof(AccountsProvider),
                    nameof(GetAccountById),
                    _draftSnapshotKeeper.TradingDay.ToJson(),
                    "Draft snapshot keeper initialized and will be used as accounts provider");
                
                var accounts = _draftSnapshotKeeper
                    .GetAccountsAsync()
                    .GetAwaiter()
                    .GetResult();

                return accounts
                    .SingleOrDefault(a => a.Id == accountId);
            }
            
            return _accountsCacheService.TryGet(accountId);
        }

        public bool TryFinishLiquidation(string accountId, string reason, string liquidationOperationId = null) =>
            _accountsCacheService.TryFinishLiquidation(accountId, reason, liquidationOperationId);
    }
}