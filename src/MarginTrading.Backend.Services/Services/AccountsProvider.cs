// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Autofac;
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
        private readonly ILifetimeScope _lifetimeScope;

        public AccountsProvider(IAccountsCacheService accountsCacheService, ILifetimeScope lifetimeScope)
        {
            _accountsCacheService = accountsCacheService;
            _lifetimeScope = lifetimeScope;
        }

        public MarginTradingAccount GetAccountById(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentNullException(nameof(accountId));
            
            using (var scope = _lifetimeScope.BeginLifetimeScope(ScopeConstants.SnapshotDraft))
            {
                if (scope.TryResolveSnapshotKeeper(out var snapshotKeeper))
                {
                    var accounts = snapshotKeeper
                        .GetAccountsAsync()
                        .GetAwaiter()
                        .GetResult();

                    return accounts
                        .SingleOrDefault(a => a.Id == accountId);
                }
            }

            return _accountsCacheService.TryGet(accountId);
        }

        public bool TryFinishLiquidation(string accountId, string reason, string liquidationOperationId = null) =>
            _accountsCacheService.TryFinishLiquidation(accountId, reason, liquidationOperationId);
    }
}