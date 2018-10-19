using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common;
using Common.Log;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;
using MarginTrading.Contract.RabbitMqMessageModels;
using MoreLinq;

namespace MarginTrading.Backend.Services
{
    public class AccountManager : TimerPeriod
    {
        private readonly AccountsCacheService _accountsCacheService;
        private readonly IConsole _console;
        private readonly MarginTradingSettings _marginSettings;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly ILog _log;
        private readonly OrdersCache _ordersCache;
        private readonly ITradingEngine _tradingEngine;
        private readonly IAccountsApi _accountsApi;
        private readonly IConvertService _convertService;
        
        private readonly IAccountMarginFreezingRepository _accountMarginFreezingRepository;
        private readonly IAccountMarginUnconfirmedRepository _accountMarginUnconfirmedRepository;

        public AccountManager(
            AccountsCacheService accountsCacheService,
            IConsole console,
            MarginTradingSettings marginSettings,
            IRabbitMqNotifyService rabbitMqNotifyService,
            ILog log,
            OrdersCache ordersCache,
            ITradingEngine tradingEngine,
            IAccountsApi accountsApi,
            IConvertService convertService,
            IAccountMarginFreezingRepository accountMarginFreezingRepository,
            IAccountMarginUnconfirmedRepository accountMarginUnconfirmedRepository) 
            : base(nameof(AccountManager), 60000, log)
        {
            _accountsCacheService = accountsCacheService;
            _console = console;
            _marginSettings = marginSettings;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _log = log;
            _ordersCache = ordersCache;
            _tradingEngine = tradingEngine;
            _accountsApi = accountsApi;
            _convertService = convertService;
            _accountMarginFreezingRepository = accountMarginFreezingRepository;
            _accountMarginUnconfirmedRepository = accountMarginUnconfirmedRepository;
        }

        public override Task Execute()
        {
            var accounts = GetAccountsToWriteStats();
            var accountsStatsMessages = GenerateAccountsStatsUpdateMessages(accounts);
            var tasks = accountsStatsMessages.Select(m => _rabbitMqNotifyService.UpdateAccountStats(m));

            return Task.WhenAll(tasks);
        }

        public override void Start()
        {
            _console.WriteLine("Starting InitAccountsCache");

            var accounts = _accountsApi.List().GetAwaiter().GetResult()
                .Select(Convert).ToDictionary(x => x.Id);

            //TODO: think about approach
            //ApplyMarginFreezing(accounts);
            
            _accountsCacheService.InitAccountsCache(accounts);
            _console.WriteLine($"Finished InitAccountsCache. Count: {accounts.Count}");

            base.Start();
        }

        private void ApplyMarginFreezing(Dictionary<string, MarginTradingAccount> accounts)
        {
            var marginFreezings = _accountMarginFreezingRepository.GetAllAsync().GetAwaiter().GetResult()
                .GroupBy(x => x.AccountId)
                .ToDictionary(x => x.Key, x => x.ToDictionary(z => z.OperationId, z => z.Amount));
            var unconfirmedMargin = _accountMarginUnconfirmedRepository.GetAllAsync().GetAwaiter().GetResult()
                .GroupBy(x => x.AccountId)
                .ToDictionary(x => x.Key, x => x.ToDictionary(z => z.OperationId, z => z.Amount));
            foreach (var account in accounts.Select(x => x.Value))
            {
                account.AccountFpl.WithdrawalFrozenMarginData = marginFreezings.TryGetValue(account.Id, out var withdrawalFrozenMargin)
                    ? withdrawalFrozenMargin
                    : new Dictionary<string, decimal>();
                account.AccountFpl.WithdrawalFrozenMargin = account.AccountFpl.WithdrawalFrozenMarginData.Sum(x => x.Value);
                account.AccountFpl.UnconfirmedMarginData = unconfirmedMargin.TryGetValue(account.Id, out var unconfirmedFrozenMargin)
                    ? unconfirmedFrozenMargin
                    : new Dictionary<string, decimal>();
                account.AccountFpl.UnconfirmedMargin = account.AccountFpl.UnconfirmedMarginData.Sum(x => x.Value);
            }
        }

        private IReadOnlyList<IMarginTradingAccount> GetAccountsToWriteStats()
        {
            var accountsIdsToWrite = Enumerable.ToHashSet(_ordersCache.GetPositions().Select(a => a.AccountId).Distinct());
            return _accountsCacheService.GetAll().Where(a => accountsIdsToWrite.Contains(a.Id)).ToList();
        }

        // todo: extract this to a cqrs process
        private IEnumerable<AccountStatsUpdateMessage> GenerateAccountsStatsUpdateMessages(
            IEnumerable<IMarginTradingAccount> accounts)
        {
            return accounts.Select(a => a.ToRabbitMqContract(_marginSettings.IsLive)).Batch(100)
                .Select(ch => new AccountStatsUpdateMessage {Accounts = ch.ToArray()});
        }

        public async Task<List<Order>> CloseAccountOrders(string accountId, string correlationId)
        {
            var positions = _ordersCache.Positions.GetPositionsByAccountIds(accountId).ToArray();
            var closedOrders = new List<Order>();

            foreach (var position in positions)
            {
                try
                {
                    var closedOrder = await _tradingEngine.ClosePositionAsync(position.Id, OriginatorType.OnBehalf, "", 
                        "Close orders for account");

                    closedOrders.Add(closedOrder);
                }
                catch (Exception e)
                {
                    await _log.WriteWarningAsync(nameof(AccountManager), "CloseAccountActiveOrders",
                        $"AccountId: {accountId}, OrderId: {position.Id}", $"Error closing order: {e.Message}");
                }
            }

            var activeOrders = _ordersCache.Active.GetOrdersByAccountIds(accountId);
            
            foreach (var order in activeOrders)
            {
                try
                {
                    var closedOrder = _tradingEngine.CancelPendingOrder(order.Id, OriginatorType.OnBehalf,
                        "Close orders for account", correlationId);
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

        private MarginTradingAccount Convert(AccountContract accountContract)
        {
            return _convertService.Convert<AccountContract, MarginTradingAccount>(accountContract,
                o => o.ConfigureMap(MemberList.Source).ForSourceMember(x => x.ModificationTimestamp, c => c.Ignore()));
        }
    }
}