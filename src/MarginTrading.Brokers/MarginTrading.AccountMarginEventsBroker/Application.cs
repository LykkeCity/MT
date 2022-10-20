// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lykke.MarginTrading.BrokerBase.Settings;
using Lykke.MarginTrading.BrokerBase;
using Lykke.Snow.Common.Correlation.RabbitMq;
using MarginTrading.AccountMarginEventsBroker.Repositories;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using MarginTrading.Backend.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountMarginEventsBroker
{
    internal class Application : BrokerApplicationBase<MarginEventMessage>
    {
        private readonly IAccountMarginEventsRepository _accountMarginEventsRepository;
        private readonly Settings _settings;

        public Application(
            RabbitMqCorrelationManager correlationManager,
            ILoggerFactory loggerFactory,
            ILogger<Application> logger,
            Settings settings,
            CurrentApplicationInfo applicationInfo,
            IAccountMarginEventsRepository accountMarginEventsRepository)
            : base(correlationManager, loggerFactory, logger, applicationInfo)
        {
            _settings = settings;
            _accountMarginEventsRepository = accountMarginEventsRepository;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.AccountMarginEvents.ExchangeName;
        public override string RoutingKey => null;

        protected override Task HandleMessage(MarginEventMessage message)
        {
            return _accountMarginEventsRepository.InsertOrReplaceAsync(new AccountMarginEvent
            {
                EventId = message.EventId,
                EventTime = message.EventTime,
                IsEventStopout = message.EventType == MarginEventTypeContract.Stopout,
                EventType = message.EventType,

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