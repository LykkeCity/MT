// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Extensions;
using MarginTrading.Backend.Services.Mappers;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Services
{
    /// <summary>
    /// Provides accounts depending on request scope: either from current system state or from trading snapshot draft
    /// </summary>
    public class AccountsProvider : IAccountsProvider
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IDraftSnapshotKeeper _draftSnapshotKeeper;
        private readonly IAccountsApi _accountsApi;
        private readonly IConvertService _convertService;
        private readonly ILog _log;

        public AccountsProvider(IAccountsCacheService accountsCacheService,
            IDraftSnapshotKeeper draftSnapshotKeeper,
            IAccountsApi accountsApi,
            IConvertService convertService,
            ILog log)
        {
            _accountsCacheService = accountsCacheService;
            _draftSnapshotKeeper = draftSnapshotKeeper;
            _accountsApi = accountsApi;
            _convertService = convertService;
            _log = log;
        }

        public MarginTradingAccount GetAccountById(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentNullException(nameof(accountId));

            return GetAccountByIdInternal(accountId);
        }

        public Task<bool> TryFinishLiquidation(string accountId, string reason, string liquidationOperationId = null) =>
            _accountsCacheService.TryFinishLiquidation(accountId, reason, liquidationOperationId);

        public async Task<MarginTradingAccount> GetActiveOrDeleted(string accountId)
        {
            var account = _accountsCacheService.TryGet(accountId);
            if (account != null) return account;

            var deletedAccount = await _accountsApi.GetById(accountId);
            if (deletedAccount != null)
            {
                return Convert(deletedAccount);
            }

            return null;
        }

        public async Task<decimal?> GetDisposableCapital(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentNullException(nameof(accountId));
            
            var account = GetAccountByIdInternal(accountId);

            var result = await _accountsApi.GetDisposableCapital(accountId,
                new GetDisposableCapitalRequest { CapitalFigures = account.ToCapitalFiguresRequestContract() });

            return result;
        }

        private MarginTradingAccount Convert(AccountContract accountContract)
        {
            var retVal = _convertService.Convert<AccountContract, MarginTradingAccount>(accountContract);
            return retVal;
        }
        
        private MarginTradingAccount GetAccountByIdInternal(string accountId)
        {
            if (!_draftSnapshotKeeper.Initialized()) 
                return _accountsCacheService.TryGet(accountId);
            
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
    }
}