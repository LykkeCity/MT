using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using MarginTrading.Core;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services
{
    public class AccountManager: IStartable
    {
        private readonly AccountsCacheService _accountsCacheService;
        private readonly IMarginTradingAccountsRepository _repository;
        private readonly IConsole _console;
        private readonly MarginSettings _marginSettings;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IAccountGroupCacheService _accountGroupCacheService;
        private readonly IMarginTradingAccountsRepository _accountsRepository;
        private readonly ITradingConditionsCacheService _tradingConditionsCacheService;
        private readonly ILog _log;
        private readonly IClientNotifyService _clientNotifyService;


        public AccountManager(AccountsCacheService accountsCacheService,
            IMarginTradingAccountsRepository repository,
            IConsole console,
            MarginSettings marginSettings,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IAccountGroupCacheService accountGroupCacheService,
            IMarginTradingAccountsRepository accountsRepository,
            ITradingConditionsCacheService tradingConditionsCacheService,
            ILog log,
            IClientNotifyService clientNotifyService)
        {
            _accountsCacheService = accountsCacheService;
            _repository = repository;
            _console = console;
            _marginSettings = marginSettings;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _accountGroupCacheService = accountGroupCacheService;
            _accountsRepository = accountsRepository;
            _tradingConditionsCacheService = tradingConditionsCacheService;
            _log = log;
            _clientNotifyService = clientNotifyService;
        }

        public void Start()
        {
            var accounts = _repository.GetAllAsync().Result.Select(MarginTradingAccount.Create).GroupBy(x => x.ClientId).ToDictionary(x => x.Key, x => x.ToArray());

            _accountsCacheService.InitAccountsCache(accounts);

            _console.WriteLine($"InitAccountsCache (clients count:{accounts.Count})");
        }

        public async Task UpdateAccountsCacheAsync(string clientId, IEnumerable<MarginTradingAccount> accounts = null)
        {
            if (accounts == null)
                accounts = (await _repository.GetAllAsync(clientId)).Select(MarginTradingAccount.Create);

            _accountsCacheService.UpdateAccountsCache(clientId, accounts);

            await _rabbitMqNotifyService.UserUpdates(false, true, new [] {clientId});
        }

        public async Task UpdateBalanceAsync(string clientId, string accountId, double amount, AccountHistoryType historyType, string comment)
        {
            var changeLimit = false;

            if (historyType == AccountHistoryType.Deposit || historyType == AccountHistoryType.Withdraw)
            {
                await CheckLimits(clientId, accountId, amount);
                changeLimit = true;
            }

            var updatedAccount = await _repository.UpdateBalanceAsync(clientId, accountId, amount, changeLimit);
            _accountsCacheService.UpdateBalance(updatedAccount);
            
            _clientNotifyService.NotifyAccountChanged(updatedAccount);

            await _rabbitMqNotifyService.AccountHistory(accountId, clientId, amount, updatedAccount.Balance, updatedAccount.WithdrawTransferLimit, historyType, comment);
        }

        private async Task CheckLimits(string clientId, string accountId, double amount)
        {
            var account = await _repository.GetAsync(clientId, accountId);

            //withdraw can not be more then limit
            if (amount < 0 && account.WithdrawTransferLimit < Math.Abs(amount))
            {
                throw new Exception(
                    $"Can not transfer {Math.Abs(amount)}. Current limit is {account.WithdrawTransferLimit}");
            }

            //limit can not be more then max after deposit
            if (amount > 0)
            {
                var accountGroup =
                    _accountGroupCacheService.GetAccountGroup(account.TradingConditionId, account.BaseAssetId);

                if (accountGroup.DepositTransferLimit > 0 && accountGroup.DepositTransferLimit < account.WithdrawTransferLimit + amount)
                {
                    throw new Exception(
                        $"Can deposit {Math.Abs(amount)}. Current deposited value is {account.WithdrawTransferLimit}. Max value is {accountGroup.DepositTransferLimit}");
                }
            }
        }

        public async Task DeleteAccountAsync(string clientId, string accountId)
        {
            await _repository.DeleteAsync(clientId, accountId);
            await UpdateAccountsCacheAsync(clientId);
        }

        //TODO: close/remove all orders
        public async Task ResetAccountAsync(string clientId, string accountId)
        {
            var account = _accountsCacheService.Get(clientId, accountId);

            await UpdateBalanceAsync(clientId, accountId, -account.Balance, AccountHistoryType.Reset,
                "Reset account");

            await UpdateBalanceAsync(clientId, accountId, LykkeConstants.DefaultDemoBalance, AccountHistoryType.Deposit,
                "Initial deposit");
        }

        public async Task AddAccountAsync(string clientId, string baseAssetId, string tradingConditionId)
        {
            var account = CreateAccount(clientId, baseAssetId, tradingConditionId);
            await _repository.AddAsync(account);
            await UpdateAccountsCacheAsync(account.ClientId);
        }

        public async Task<MarginTradingAccount[]> CreateDefaultAccounts(string clientId, string tradingConditionsId = null)
        {
            var existingAccounts = (await _accountsRepository.GetAllAsync(clientId)).ToList();

            if (existingAccounts.Any())
            {
                var accounts = existingAccounts.Select(MarginTradingAccount.Create).ToArray();
                _accountsCacheService.UpdateAccountsCache(clientId, accounts);
                return accounts;
            }

            if (string.IsNullOrEmpty(tradingConditionsId))
                tradingConditionsId = GetTradingConditions();

            var baseAssets = GetBaseAssets(tradingConditionsId);

            var newAccounts = new List<MarginTradingAccount>();

            foreach (var baseAsset in baseAssets)
            {
                try
                {
                    var account = CreateAccount(clientId, baseAsset, tradingConditionsId);
                    await _repository.AddAsync(account);
                    newAccounts.Add(account);
                }
                catch (Exception e)
                {
                    await _log.WriteErrorAsync(nameof(AccountManager), "Create default accounts",
                        $"clientId={clientId}, tradingConditionsId={tradingConditionsId}", e);
                }
            }

            await UpdateAccountsCacheAsync(clientId, newAccounts);

            return newAccounts.ToArray();
        }

        #region Helpers

        private string[] GetBaseAssets(string tradingConditionsId)
        {
            var accountGroups =
                _accountGroupCacheService.GetAllAccountGroups().Where(g => g.TradingConditionId == tradingConditionsId);
            var baseAssets = accountGroups.Select(g => g.BaseAssetId).Distinct().ToArray();

            if (!baseAssets.Any())
                throw new Exception(
                    $"No account groups found for trading conditions {tradingConditionsId}");

            return baseAssets;
        }

        private string GetTradingConditions()
        {
            //use default trading conditions for demo
            if (!_marginSettings.IsLive)
            {
                var tradingConditions = _tradingConditionsCacheService.GetAllTradingConditions();
                var defaultConditions = tradingConditions.FirstOrDefault(item => item.IsDefault);

                if (defaultConditions == null)
                    throw new Exception("No default trading conditions set for demo");
                else
                    return defaultConditions.Id;
            }
            else
            {
                throw new Exception("No trading conditions found");
            }
        }

        private MarginTradingAccount CreateAccount(string clientId, string baseAssetId, string tradingConditionId)
        {
            var id = $"{(_marginSettings.IsLive ? string.Empty : _marginSettings.DemoAccountIdPrefix)}{Guid.NewGuid():N}";
            var initialBalance = _marginSettings.IsLive ? 0 : LykkeConstants.DefaultDemoBalance;

            return new MarginTradingAccount
            {
                Id = id,
                BaseAssetId = baseAssetId,
                ClientId = clientId,
                Balance = initialBalance,
                TradingConditionId = tradingConditionId
            };
        }

        #endregion
    }
}