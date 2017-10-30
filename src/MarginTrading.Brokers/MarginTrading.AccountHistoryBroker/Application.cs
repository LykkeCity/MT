﻿using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.AccountHistoryBroker.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.AccountHistoryBroker
{
    internal class Application : BrokerApplicationBase<AccountHistoryBackendContract>
    {
        private readonly IMarginTradingAccountHistoryRepository _accountHistoryRepository;
        private readonly IAccountTransactionsReportsRepository _accountTransactionsReportsRepository;
        private readonly MarginSettings _settings;

        public Application(IMarginTradingAccountHistoryRepository accountHistoryRepository, ILog logger,
            MarginSettings settings, CurrentApplicationInfo applicationInfo,
            IAccountTransactionsReportsRepository accountTransactionsReportsRepository)
            : base(logger, applicationInfo)
        {
            _accountHistoryRepository = accountHistoryRepository;
            _settings = settings;
            _accountTransactionsReportsRepository = accountTransactionsReportsRepository;
        }

        protected override RabbitMqSubscriptionSettings GetRabbitMqSubscriptionSettings()
        {
            var exchangeName = _settings.RabbitMqQueues.AccountHistory.ExchangeName;
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = _settings.MtRabbitMqConnString,
                QueueName = QueueHelper.BuildQueueName(exchangeName, _settings.Env),
                ExchangeName = exchangeName,
                IsDurable = true
            };
        }

        protected override Task HandleMessage(AccountHistoryBackendContract accountHistoryContract)
        {
            var accountHistory = accountHistoryContract.ToAccountHistoryContract();
            var accountTransactionReport = new AccountTransactionsReportsEntity
            {
                AccountId = accountHistoryContract.AccountId,
                ClientId = accountHistoryContract.ClientId,
                Comment = accountHistoryContract.Comment,
                Id = accountHistoryContract.Id,
                Amount = (double) accountHistoryContract.Amount,
                Balance = (double) accountHistoryContract.Balance,
                Date = accountHistoryContract.Date,
                Type = accountHistoryContract.Type.ToString(),
                WithdrawTransferLimit = (double) accountHistoryContract.WithdrawTransferLimit,
            };
            
            return Task.WhenAll(
                _accountHistoryRepository.AddAsync(accountHistory),
                _accountTransactionsReportsRepository.InsertOrReplaceAsync(accountTransactionReport));
        }
    }
}