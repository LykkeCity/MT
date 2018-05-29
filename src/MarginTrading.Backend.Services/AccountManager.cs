using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.Settings;
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

        public AccountManager(AccountsCacheService accountsCacheService, IConsole console,
            MarginTradingSettings marginSettings, IRabbitMqNotifyService rabbitMqNotifyService, ILog log,
            OrdersCache ordersCache, ITradingEngine tradingEngine, IAccountsApi accountsApi,
            IConvertService convertService) :
            base(nameof(AccountManager), 60000, log)
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

            _accountsCacheService.InitAccountsCache(accounts);
            _console.WriteLine($"Finished InitAccountsCache. Count: {accounts.Count}");

            base.Start();
        }

        private IReadOnlyList<IMarginTradingAccount> GetAccountsToWriteStats()
        {
            var accountsIdsToWrite = Enumerable.ToHashSet(_ordersCache.GetActive().Select(a => a.AccountId).Distinct());
            return _accountsCacheService.GetAll().Where(a => accountsIdsToWrite.Contains(a.Id)).ToList();
        }

        // todo: extract this to a cqrs process
        private IEnumerable<AccountStatsUpdateMessage> GenerateAccountsStatsUpdateMessages(
            IEnumerable<IMarginTradingAccount> accounts)
        {
            return accounts.Select(a => a.ToRabbitMqContract(_marginSettings.IsLive)).Batch(100)
                .Select(ch => new AccountStatsUpdateMessage {Accounts = ch.ToArray()});
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

        private MarginTradingAccount Convert(AccountContract accountContract)
        {
            return _convertService.Convert<AccountContract, MarginTradingAccount>(accountContract,
                o => o.ConfigureMap(MemberList.Source).ForSourceMember(x => x.ModificationTimestamp, c => c.Ignore()));
        }
    }
}