using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.RabbitMqMessages;
using MarginTrading.Core.Settings;
using MarginTrading.MarginEventsBroker.AzureRepositories;

namespace MarginTrading.MarginEventsBroker
{
    internal class Application : BrokerApplicationBase<AccountMarginEventMessage>
    {
        private readonly IMarginEventsReportsRepository _marginEventsReportsRepository;
        private readonly MarginSettings _settings;

        public Application(ILog logger, MarginSettings settings,
            CurrentApplicationInfo applicationInfo,
            IMarginEventsReportsRepository marginEventsReportsRepository)
            : base(logger, applicationInfo)
        {
            _settings = settings;
            _marginEventsReportsRepository = marginEventsReportsRepository;
        }

        protected override RabbitMqSubscriptionSettings GetRabbitMqSubscriptionSettings()
        {
            var exchangeName = _settings.RabbitMqQueues.AccountMarginEvents.ExchangeName;
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = _settings.MtRabbitMqConnString,
                QueueName = $"{exchangeName}.{_applicationInfo.ApplicationName}.{_settings.Env ?? "DefaultEnv"}",
                ExchangeName = _settings.RabbitMqQueues.AccountHistory.ExchangeName,
                IsDurable = true
            };
        }

        protected override Task HandleMessage(AccountMarginEventMessage message)
        {
            return _marginEventsReportsRepository.InsertOrReplaceAsync(new MarginEventReport
            {
                EventId = message.EventId,
                EventTime = message.EventTime,
                IsEventStopout = message.IsEventStopout,

                ClientId = message.ClientId,
                AccountId = message.AccountId,
                TradingConditionId = message.TradingConditionId,
                BaseAssetId = message.BaseAssetId,
                Balance = message.Balance,
                WithdrawTransferLimit = message.WithdrawTransferLimit,

                MarginCall = message.MarginCall,
                StopOut = message.StopOut,
                TotalCapital = message.TotalCapital,
                FreeMargin = message.FreeMargin,
                MarginAvailable = message.MarginAvailable,
                UsedMargin = message.UsedMargin,
                MarginInit = message.MarginInit,
                PnL = message.PnL,
                OpenPositionsCount = message.OpenPositionsCount,
                MarginUsageLevel = message.MarginUsageLevel,
            });
        }
    }
}