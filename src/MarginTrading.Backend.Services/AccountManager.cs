using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Contract.RabbitMqMessageModels;
using MoreLinq;

namespace MarginTrading.Backend.Services
{
    public class AccountManager: TimerPeriod 
    {
        private readonly AccountsCacheService _accountsCacheService;
        private readonly IMarginTradingAccountsRepository _repository;
        private readonly IConsole _console;
        private readonly MarginTradingSettings _marginSettings;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IClientNotifyService _clientNotifyService;
        private readonly ILog _log;
        private readonly OrdersCache _ordersCache;
        private readonly IEventChannel<AccountBalanceChangedEventArgs> _acountBalanceChangedEventChannel;
        private readonly ITradingEngine _tradingEngine;

        private static readonly ConcurrentDictionary<int, SemaphoreSlim> Semaphores = new ConcurrentDictionary<int, SemaphoreSlim>();
        
        public AccountManager(AccountsCacheService accountsCacheService,
            IMarginTradingAccountsRepository repository,
            IConsole console,
            MarginTradingSettings marginSettings,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IClientNotifyService clientNotifyService,
            ILog log,
            OrdersCache ordersCache,
            IEventChannel<AccountBalanceChangedEventArgs> acountBalanceChangedEventChannel,
            ITradingEngine tradingEngine)
            : base(nameof(AccountManager), 60000, log)
        {
            _accountsCacheService = accountsCacheService;
            _repository = repository;
            _console = console;
            _marginSettings = marginSettings;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _log = log;
            _clientNotifyService = clientNotifyService;
            _ordersCache = ordersCache;
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
        
        public override void Start()
        {
            var accounts = _repository.GetAllAsync().GetAwaiter().GetResult()
                .Select(MarginTradingAccount.Create).GroupBy(x => x.ClientId).ToDictionary(x => x.Key, x => x.ToArray());

            _accountsCacheService.InitAccountsCache(accounts);
            _console.WriteLine($"InitAccountsCache (clients count:{accounts.Count})");

            base.Start();
        }

        private IReadOnlyList<IMarginTradingAccount> GetAccountsToWriteStats()
        {
            var accountsIdsToWrite = Enumerable.ToHashSet(_ordersCache.GetActive().Select(a => a.AccountId).Distinct());
            return _accountsCacheService.GetAll().Where(a => accountsIdsToWrite.Contains(a.Id)).ToList();
        }

        private IEnumerable<AccountStatsUpdateMessage> GenerateAccountsStatsUpdateMessages(IReadOnlyList<IMarginTradingAccount> accounts)
        {
            var accountStats = accounts.Select(a => a.ToRabbitMqContract(_marginSettings.IsLive));

            var chunks = accountStats.Batch(100);

            foreach (var chunk in chunks)
            {
                yield return new AccountStatsUpdateMessage {Accounts = chunk.ToArray()};
            }
        }
        
        #endregion
       

        public async Task<string> UpdateBalanceAsync(IMarginTradingAccount account, decimal amount, AccountHistoryType historyType, 
            string comment, string eventSourceId = null, bool changeTransferLimit = false, string auditLog = null)
        {
            var semaphore = GetSemaphore(account);

            await semaphore.WaitAsync();

            try
            {
                var updatedAccount =
                    await _repository.UpdateBalanceAsync(account.ClientId, account.Id, amount, changeTransferLimit);
                _acountBalanceChangedEventChannel.SendEvent(this, new AccountBalanceChangedEventArgs(updatedAccount));
                //todo: move to separate event consumers
                _accountsCacheService.UpdateBalance(updatedAccount);
                _clientNotifyService.NotifyAccountUpdated(updatedAccount);

                var transactionId = Guid.NewGuid().ToString("N");
                
                await _rabbitMqNotifyService.AccountHistory(
                    transactionId,
                    account.Id,
                    account.ClientId,
                    amount,
                    updatedAccount.Balance,
                    updatedAccount.WithdrawTransferLimit,
                    historyType,
                    comment,
                    eventSourceId, 
                    auditLog);

                return transactionId;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<List<IOrder>> CloseAccountOrders(string accountId)
        {
            var openedOrders = _ordersCache.ActiveOrders.GetOrdersByAccountIds(accountId).ToArray();
            var closedOrders = new List<IOrder>();

            foreach (var order in openedOrders)
            {
                try
                {
                    var closedOrder = await _tradingEngine.CloseActiveOrderAsync(order.Id,
                        OrderCloseReason.ClosedByBroker, "Close orders for account");
                    
                    closedOrders.Add(closedOrder);
                }
                catch (Exception e)
                {
                    await _log.WriteWarningAsync(nameof(AccountManager), "CloseAccountActiveOrders",
                        $"AccountId: {accountId}, OrderId: {order.Id}", $"Error closing order: {e.Message}");
                }
            }

            var pendingOrders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(accountId);
            
            foreach (var order in pendingOrders)
            {
                try
                {
                    var closedOrder = _tradingEngine.CancelPendingOrder(order.Id, OrderCloseReason.CanceledByBroker,
                        "Close orders for account");
                    closedOrders.Add(closedOrder);
                }
                catch (Exception e)
                {
                    await _log.WriteWarningAsync(nameof(AccountManager), "CloseAccountOrders",
                        $"AccountId: {accountId}, OrderId: {order.Id}", $"Error cancelling order: {e.Message}");
                }
            }

            return closedOrders;
        }

        #region Helpers

        private SemaphoreSlim GetSemaphore(IMarginTradingAccount account)
        {
            var hash = account.Id.GetHashCode() % 100;

            return Semaphores.GetOrAdd(hash, new SemaphoreSlim(1, 1));
        }

        #endregion
        
    }
}