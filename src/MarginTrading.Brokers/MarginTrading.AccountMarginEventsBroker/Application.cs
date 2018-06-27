using System.Threading.Tasks;
using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.AccountMarginEventsBroker.Repositories;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using MarginTrading.Backend.Contracts.Events;

namespace MarginTrading.AccountMarginEventsBroker
{
    internal class Application : BrokerApplicationBase<MarginEventMessage>
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

        protected override Task HandleMessage(MarginEventMessage message)
        {
            return _accountMarginEventsReportsRepository.InsertOrReplaceAsync(new AccountMarginEventReport
            {
                EventId = message.EventId,
                EventTime = message.EventTime,
                IsEventStopout = message.EventType == MarginEventTypeContract.Stopout,

                ClientId = message.ClientId,
                AccountId = message.AccountId,
                TradingConditionId = message.TradingConditionId,
                BaseAssetId = message.BaseAssetId,
                Balance = (double) message.Balance,
                WithdrawTransferLimit = (double) message.WithdrawTransferLimit,

                MarginCall = (double) message.MarginCall1Level,
                StopOut = (double) message.StopOutLevel,
                TotalCapital = (double) message.TotalCapital,
                FreeMargin = (double) message.FreeMargin,
                MarginAvailable = (double) message.MarginAvailable,
                UsedMargin = (double) message.UsedMargin,
                MarginInit = (double) message.MarginInit,
                PnL = (double) message.PnL,
                OpenPositionsCount = (double) message.OpenPositionsCount,
                MarginUsageLevel = (double) message.MarginUsageLevel
            });
        }
    }
}