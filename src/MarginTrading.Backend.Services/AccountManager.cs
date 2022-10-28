// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common;
using Common.Log;
using FluentScheduler;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Extensions;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;
using MarginTrading.Contract.RabbitMqMessageModels;
using Microsoft.Extensions.Internal;
using MoreLinq;

namespace MarginTrading.Backend.Services
{
    public class AccountManager : TimerPeriod
    {
        private readonly AccountsCacheService _accountsCacheService;
        private readonly MarginTradingSettings _marginSettings;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly ILog _log;
        private readonly OrdersCache _ordersCache;
        private readonly ITradingEngine _tradingEngine;
        private readonly IAccountsApi _accountsApi;
        private readonly IAccountBalanceHistoryApi _accountBalanceHistoryApi;
        private readonly IConvertService _convertService;
        private readonly IDateService _dateService;
        private readonly ISystemClock _systemClock;

        private readonly IAccountMarginFreezingRepository _accountMarginFreezingRepository;
        private readonly IAccountMarginUnconfirmedRepository _accountMarginUnconfirmedRepository;

        public AccountManager(
            AccountsCacheService accountsCacheService,
            MarginTradingSettings marginSettings,
            IRabbitMqNotifyService rabbitMqNotifyService,
            ILog log,
            OrdersCache ordersCache,
            ITradingEngine tradingEngine,
            IAccountsApi accountsApi,
            IAccountBalanceHistoryApi accountBalanceHistoryApi,
            IConvertService convertService,
            IDateService dateService,
            ISystemClock systemClock,
            IAccountMarginFreezingRepository accountMarginFreezingRepository,
            IAccountMarginUnconfirmedRepository accountMarginUnconfirmedRepository)
            : base(nameof(AccountManager), 60000, log)
        {
            _accountsCacheService = accountsCacheService;
            _marginSettings = marginSettings;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _log = log;
            _ordersCache = ordersCache;
            _tradingEngine = tradingEngine;
            _accountsApi = accountsApi;
            _accountBalanceHistoryApi = accountBalanceHistoryApi;
            _convertService = convertService;
            _dateService = dateService;
            _systemClock = systemClock;
            _accountMarginFreezingRepository = accountMarginFreezingRepository;
            _accountMarginUnconfirmedRepository = accountMarginUnconfirmedRepository;
        }

        public override Task Execute()
        {
            //TODO: to think if we need this process, at the current moment it is not used and only increases load on RabbitMq
            //            var accounts = GetAccountsToWriteStats();
            //            var accountsStatsMessages = GenerateAccountsStatsUpdateMessages(accounts);
            //            var tasks = accountsStatsMessages.Select(m => _rabbitMqNotifyService.UpdateAccountStats(m));
            //
            //            return Task.WhenAll(tasks);
            return Task.CompletedTask;
        }

        private async Task<Dictionary<string, MarginTradingAccount>> GetAccounts()
        {
            var accountsTask = _accountsApi.List();
            var onDate = _systemClock.UtcNow.UtcDateTime.Date; 
            var balanceChangesTask = _accountBalanceHistoryApi.ByDate(onDate, onDate.AddDays(1));

            await Task.WhenAll(accountsTask, balanceChangesTask);

            var accounts = await accountsTask;
            var balanceChanges = await balanceChangesTask;
            
            var result = accounts.Select(Convert).ToDictionary(x => x.Id);
            result.ForEach(x =>
            {
                var account = x.Value;
                if (balanceChanges.ContainsKey(x.Key))
                {
                    var accountBalanceChanges = balanceChanges[x.Key];
                    var firstBalanceChange = accountBalanceChanges.OrderBy(b => b.ChangeTimestamp).FirstOrDefault();
                    account.TodayStartBalance = firstBalanceChange !=null
                        ? firstBalanceChange.Balance - firstBalanceChange.ChangeAmount
                        : account.Balance;
                    account.TodayRealizedPnL = accountBalanceChanges.GetTotalByType(AccountBalanceChangeReasonTypeContract.RealizedPnL);
                    account.TodayUnrealizedPnL = accountBalanceChanges.GetTotalByType(AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL);
                    account.TodayDepositAmount = accountBalanceChanges.GetTotalByType(AccountBalanceChangeReasonTypeContract.Deposit);
                    account.TodayWithdrawAmount = accountBalanceChanges.GetTotalByType(AccountBalanceChangeReasonTypeContract.Withdraw);
                    account.TodayCommissionAmount = accountBalanceChanges.GetTotalByType(AccountBalanceChangeReasonTypeContract.Commission);
                    account.TodayOtherAmount = accountBalanceChanges.Where(x => !new[]
                    {
                        AccountBalanceChangeReasonTypeContract.RealizedPnL,
                        // AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL, // TODO: why not (copied from account management)?
                        AccountBalanceChangeReasonTypeContract.Deposit,
                        AccountBalanceChangeReasonTypeContract.Withdraw,
                        AccountBalanceChangeReasonTypeContract.Commission,
                    }.Contains(x.ReasonType)).Sum(x => x.ChangeAmount);
                }
                else
                {
                    account.TodayStartBalance = account.Balance;
                }
            });
            return result;
        }

        public override void Start()
        {
            _log.WriteInfo(nameof(Start), nameof(AccountManager), "Starting InitAccountsCache");

            var accounts = GetAccounts().GetAwaiter().GetResult();

            _accountsCacheService.InitAccountsCache(accounts);
            _log.WriteInfo(nameof(Start), nameof(AccountManager), $"Finished InitAccountsCache. Count: {accounts.Count}");
            
            base.Start();
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
            return accounts.Select(a => a.ToRabbitMqContract()).Batch(100)
                .Select(ch => new AccountStatsUpdateMessage { Accounts = ch.ToArray() });
        }

        private MarginTradingAccount Convert(AccountContract accountContract)
        {
            var retVal = _convertService.Convert<AccountContract, MarginTradingAccount>(accountContract);
            // The line below is related to LT-1786 ticket.
            // After restarting core we cannot have LastBalanceChangeTime less than in donut's cache to avoid infinite account reloading
            retVal.LastBalanceChangeTime = _dateService.Now();
            return retVal;
        }
    }
}