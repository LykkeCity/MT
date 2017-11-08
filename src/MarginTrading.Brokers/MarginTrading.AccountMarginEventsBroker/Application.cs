using System.Threading.Tasks;
using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.RabbitMqMessageModels;
using MarginTrading.AccountMarginEventsBroker.Repositories;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using System;

namespace MarginTrading.AccountMarginEventsBroker
{
    internal class Application : BrokerApplicationBase<AccountMarginEventMessage>
    {
        private readonly IAccountMarginEventsReportsRepository _accountMarginEventsReportsRepository;
        private readonly Settings _settings;

        public Application(ILog logger, Settings settings,
            CurrentApplicationInfo applicationInfo,
            IAccountMarginEventsReportsRepository accountMarginEventsReportsRepository,
            ISlackNotificationsSender slackNotificationsSender)
            : base(logger, slackNotificationsSender, applicationInfo)
        {
            _settings = settings;
            _accountMarginEventsReportsRepository = accountMarginEventsReportsRepository;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.AccountMarginEvents.ExchangeName;

        protected override Task HandleMessage(AccountMarginEventMessage message)
        {
            return _accountMarginEventsReportsRepository.InsertOrReplaceAsync(new AccountMarginEventReport
            {
                EventId = message.EventId,
                EventTime = message.EventTime,
                IsEventStopout = message.IsEventStopout,

                ClientId = message.ClientId,
                AccountId = message.AccountId,
                TradingConditionId = message.TradingConditionId,
                BaseAssetId = message.BaseAssetId,
                Balance = Math.Round(message.Balance, 10),
                WithdrawTransferLimit = Math.Round(message.WithdrawTransferLimit, 10),

                MarginCall = Math.Round(message.MarginCall, 10),
                StopOut = Math.Round(message.StopOut, 10),
                TotalCapital = Math.Round(message.TotalCapital, 10),
                FreeMargin = Math.Round(message.FreeMargin, 10),
                MarginAvailable = Math.Round(message.MarginAvailable, 10),
                UsedMargin = Math.Round(message.UsedMargin, 10),
                MarginInit = Math.Round(message.MarginInit, 10),
                PnL = Math.Round(message.PnL, 10),
                OpenPositionsCount = Math.Round(message.OpenPositionsCount, 10),
                MarginUsageLevel = Math.Round(message.MarginUsageLevel, 10),
            });
        }
    }
}