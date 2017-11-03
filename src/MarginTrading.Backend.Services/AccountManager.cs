using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Contract.RabbitMqMessageModels;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
    public class AccountManager: TimerPeriod 
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
        private readonly OrdersCache _ordersCache;
        private readonly IUpdatedAccountsTrackingService _updatedAccountsTrackingService;
        private readonly IEventChannel<AccountBalanceChangedEventArgs> _acountBalanceChangedEventChannel;
        private readonly ITradingEngine _tradingEngine;


        public AccountManager(AccountsCacheService accountsCacheService,
            IMarginTradingAccountsRepository repository,
            IConsole console,
            MarginSettings marginSettings,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IAccountGroupCacheService accountGroupCacheService,
            IMarginTradingAccountsRepository accountsRepository,
            ITradingConditionsCacheService tradingConditionsCacheService,
            ILog log,
            IClientNotifyService clientNotifyService,
            OrdersCache ordersCache,
            IUpdatedAccountsTrackingService updatedAccountsTrackingService,
            IEventChannel<AccountBalanceChangedEventArgs> acountBalanceChangedEventChannel,
            ITradingEngine tradingEngine)
            : base(nameof(AccountManager), 10000, log)
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
            _ordersCache = ordersCache;
            _updatedAccountsTrackingService = updatedAccountsTrackingService;
            _acountBalanceChangedEventChannel = acountBalanceChangedEventChannel;
            _tradingEngine = tradingEngine;
        }

        
        #region TimePeriod
        
        public override Task Execute()
        {
            var accounts = GetAccountsToWriteStats();
            var accountsStatsMessages = GenerateAccountsStatsUpdateMessages(accounts);
            var tasks = accountsStatsMessages.Select(m => _rabbitMqNotifyService.UpdateAccountStats(m));

            return Task.WhenAll(tasks);
        }

        private IReadOnlyList<IMarginTradingAccount> GetAccountsToWriteStats()
        {
            var accountsIdsToWrite = _ordersCache.GetActive().Select(a => a.AccountId)
                .Concat(_updatedAccountsTrackingService.GetAccounts())
                .Distinct();
            return _accountsCacheService.GetAll().Join(accountsIdsToWrite, a => a.Id, a => a, (account, id) => account)
                .ToList();
        }

        private IEnumerable<AccountStatsUpdateMessage> GenerateAccountsStatsUpdateMessages(IReadOnlyList<IMarginTradingAccount> accounts)
        {
            var accountStats = accounts.Select(a => a.ToRabbitMqContract(_marginSettings.IsLive));

            var chunks = accountStats.ToChunks(100);

            foreach (var chunk in chunks)
            {
                yield return new AccountStatsUpdateMessage {Accounts = chunk.ToArray()};
            }
        }
        
        #endregion

        public override void Start()
        {
            var accounts = _repository.GetAllAsync().GetAwaiter().GetResult()
                .Select(MarginTradingAccount.Create).GroupBy(x => x.ClientId).ToDictionary(x => x.Key, x => x.ToArray());

            _accountsCacheService.InitAccountsCache(accounts);
            _console.WriteLine($"InitAccountsCache (clients count:{accounts.Count})");

            base.Start();
        }

        private async Task ProcessAccountsSetChange(string clientId, IReadOnlyList<MarginTradingAccount> allClientsAccounts = null)
        {
            if (allClientsAccounts == null)
            {
                allClientsAccounts = (await _repository.GetAllAsync(clientId)).Select(MarginTradingAccount.Create).ToList();
            }

            _accountsCacheService.UpdateAccountsCache(clientId, allClientsAccounts);
            await _rabbitMqNotifyService.UserUpdates(false, true, new[] {clientId});
        }

        public async Task UpdateBalanceAsync(IMarginTradingAccount account, decimal amount, AccountHistoryType historyType, string comment, bool changeTransferLimit = false)
        {
            if (historyType == AccountHistoryType.Deposit && changeTransferLimit)
            {
                CheckDepositLimits(account, amount);
            }

            if (changeTransferLimit)
            {
                CheckTransferLimits(account, amount);
            }

            var updatedAccount = await _repository.UpdateBalanceAsync(account.ClientId, account.Id, amount, changeTransferLimit);
            _acountBalanceChangedEventChannel.SendEvent(this, new AccountBalanceChangedEventArgs(updatedAccount));
            //todo: move to separate event consumers
            _accountsCacheService.UpdateBalance(updatedAccount);
            _clientNotifyService.NotifyAccountUpdated(updatedAccount);

            await _rabbitMqNotifyService.AccountHistory(account.Id, account.ClientId, amount, updatedAccount.Balance,
                updatedAccount.WithdrawTransferLimit, historyType, comment);
        }

        private void CheckDepositLimits(IMarginTradingAccount account, decimal amount)
        {
            //limit can not be more then max after deposit
            if (amount > 0)
            {
                var accountGroup =
                    _accountGroupCacheService.GetAccountGroup(account.TradingConditionId, account.BaseAssetId);

                if (accountGroup.DepositTransferLimit > 0 && accountGroup.DepositTransferLimit < account.Balance + amount)
                {
                    throw new Exception(
                        $"Margin Trading is in beta testing. The cash-ins are temporarily limited when Total Capital exceeds {accountGroup.DepositTransferLimit} {accountGroup.BaseAssetId}. Thank you for using Lykke Margin Trading, the limit will be cancelled soon!");
                }
            }
        }

        private void CheckTransferLimits(IMarginTradingAccount account, decimal amount)
        {
            //withdraw can not be more then limit
            if (amount < 0 && account.WithdrawTransferLimit < Math.Abs(amount))
            {
                throw new Exception(
                    $"Can not transfer {Math.Abs(amount)}. Current limit is {account.WithdrawTransferLimit}");
            }
        }

        public async Task DeleteAccountAsync(string clientId, string accountId)
        {
            var account = _accountsCacheService.Get(clientId, accountId);
            await _repository.DeleteAsync(clientId, accountId);
            await ProcessAccountsSetChange(clientId);
            await _rabbitMqNotifyService.AccountDeleted(account);
        }

        //TODO: close/remove all orders
        public async Task ResetAccountAsync(string clientId, string accountId)
        {
            var account = _accountsCacheService.Get(clientId, accountId);

            await UpdateBalanceAsync(account, LykkeConstants.DefaultDemoBalance - account.Balance,
                AccountHistoryType.Reset,
                "Reset account");
        }

        public async Task AddAccountAsync(string clientId, string baseAssetId, string tradingConditionId)
        {
            var account = CreateAccount(clientId, baseAssetId, tradingConditionId);
            await _repository.AddAsync(account);
            await ProcessAccountsSetChange(account.ClientId);
            await _rabbitMqNotifyService.AccountCreated(account);
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
                    await _rabbitMqNotifyService.AccountCreated(account);
                    newAccounts.Add(account);
                }
                catch (Exception e)
                {
                    await _log.WriteErrorAsync(nameof(AccountManager), "Create default accounts",
                        $"clientId={clientId}, tradingConditionsId={tradingConditionsId}", e);
                }
            }

            await ProcessAccountsSetChange(clientId, newAccounts);

            return newAccounts.ToArray();
        }

        public async Task<List<IOrder>> CloseAccountOrders(string accountId, OrderCloseReason reason)
        {
            var openedOrders = _ordersCache.ActiveOrders.GetOrdersByAccountIds(accountId).ToArray();
            var closedOrders = new List<IOrder>();

            foreach (var order in openedOrders)
            {
                try
                {
                    var closedOrder = await _tradingEngine.CloseActiveOrderAsync(order.Id, reason);
                    closedOrders.Add(closedOrder);
                }
                catch (Exception e)
                {
                    await _log.WriteWarningAsync(nameof(AccountManager), "CloseAccountActiveOrders",
                        $"AccountId: {accountId}, OrderId: {order.Id}", $"Error closing order: {e.Message}");
                }
            }

            return closedOrders;
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

        [Pure]
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