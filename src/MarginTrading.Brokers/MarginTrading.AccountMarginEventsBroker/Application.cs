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
                Balance = message.Balance.ToRoundedDecimal(),
                WithdrawTransferLimit = message.WithdrawTransferLimit.ToRoundedDecimal(),

                MarginCall = message.MarginCall.ToRoundedDecimal(),
                StopOut = message.StopOut.ToRoundedDecimal(),
                TotalCapital = message.TotalCapital.ToRoundedDecimal(),
                FreeMargin = message.FreeMargin.ToRoundedDecimal(),
                MarginAvailable = message.MarginAvailable.ToRoundedDecimal(),
                UsedMargin = message.UsedMargin.ToRoundedDecimal(),
                MarginInit = message.MarginInit.ToRoundedDecimal(),
                PnL = message.PnL.ToRoundedDecimal(),
                OpenPositionsCount = message.OpenPositionsCount.ToRoundedDecimal(),
                MarginUsageLevel = message.MarginUsageLevel.ToRoundedDecimal()
            });
        }
    }
}