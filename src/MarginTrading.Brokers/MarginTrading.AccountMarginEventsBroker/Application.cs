using System.Threading.Tasks;
using Common.Log;
using Lykke.MarginTrading.BrokerBase.Settings;
using Lykke.SlackNotifications;
using Lykke.MarginTrading.BrokerBase;
using MarginTrading.AccountMarginEventsBroker.Repositories;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using MarginTrading.Backend.Contracts.Events;

namespace MarginTrading.AccountMarginEventsBroker
{
    internal class Application : BrokerApplicationBase<MarginEventMessage>
    {
        private readonly IAccountMarginEventsRepository _accountMarginEventsRepository;
        private readonly Settings _settings;

        public Application(ILog logger, Settings settings,
            CurrentApplicationInfo applicationInfo,
            IAccountMarginEventsRepository accountMarginEventsRepository,
            ISlackNotificationsSender slackNotificationsSender)
            : base(logger, slackNotificationsSender, applicationInfo)
        {
            _settings = settings;
            _accountMarginEventsRepository = accountMarginEventsRepository;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.AccountMarginEvents.ExchangeName;
        protected override string RoutingKey => null;

        protected override Task HandleMessage(MarginEventMessage message)
        {
            return _accountMarginEventsRepository.InsertOrReplaceAsync(new AccountMarginEvent
            {
                EventId = message.EventId,
                EventTime = message.EventTime,
                IsEventStopout = message.EventType == MarginEventTypeContract.Stopout,

                AccountId = message.AccountId,
                TradingConditionId = message.TradingConditionId,
                BaseAssetId = message.BaseAssetId,
                Balance = message.Balance,
                WithdrawTransferLimit = message.WithdrawTransferLimit,

                MarginCall = message.MarginCall1Level,
                StopOut = message.StopOutLevel,
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