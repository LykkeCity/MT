﻿using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.AccountMarginEventsBroker.AzureRepositories;
using MarginTrading.Backend.Core.RabbitMqMessages;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.AccountMarginEventsBroker
{
    internal class Application : BrokerApplicationBase<AccountMarginEventMessage>
    {
        private readonly IAccountMarginEventsReportsRepository _accountMarginEventsReportsRepository;
        private readonly MarginSettings _settings;

        public Application(ILog logger, MarginSettings settings,
            CurrentApplicationInfo applicationInfo,
            IAccountMarginEventsReportsRepository accountMarginEventsReportsRepository)
            : base(logger, applicationInfo)
        {
            _settings = settings;
            _accountMarginEventsReportsRepository = accountMarginEventsReportsRepository;
        }

        protected override RabbitMqSubscriptionSettings GetRabbitMqSubscriptionSettings()
        {
            var exchangeName = _settings.RabbitMqQueues.AccountMarginEvents.ExchangeName;
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = _settings.MtRabbitMqConnString,
                QueueName = QueueHelper.BuildQueueName(exchangeName, _settings.Env),
                ExchangeName = exchangeName,
                IsDurable = true
            };
        }

        protected override Task HandleMessage(AccountMarginEventMessage message)
        {
            return _accountMarginEventsReportsRepository.InsertOrReplaceAsync(new AccountMarginEventReportEntity
            {
                EventId = message.EventId,
                EventTime = message.EventTime,
                IsEventStopout = message.IsEventStopout,

                ClientId = message.ClientId,
                AccountId = message.AccountId,
                TradingConditionId = message.TradingConditionId,
                BaseAssetId = message.BaseAssetId,
                Balance = (double) message.Balance,
                WithdrawTransferLimit = (double) message.WithdrawTransferLimit,

                MarginCall = (double) message.MarginCall,
                StopOut = (double) message.StopOut,
                TotalCapital = (double) message.TotalCapital,
                FreeMargin = (double) message.FreeMargin,
                MarginAvailable = (double) message.MarginAvailable,
                UsedMargin = (double) message.UsedMargin,
                MarginInit = (double) message.MarginInit,
                PnL = (double) message.PnL,
                OpenPositionsCount = (double) message.OpenPositionsCount,
                MarginUsageLevel = (double) message.MarginUsageLevel,
            });
        }
    }
}