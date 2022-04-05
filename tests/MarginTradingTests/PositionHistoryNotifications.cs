// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.ExchangeConnector;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTradingTests
{
    public class PositionHistoryNotifications : IRabbitMqNotifyService
    {
        private readonly List<PositionHistoryEvent> _container;

        public PositionHistoryNotifications(List<PositionHistoryEvent> container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }
        public async Task OrderHistory(Order order, OrderUpdateType orderUpdateType, string activitiesMetadata = null)
        {
            throw new System.NotImplementedException();
        }

        public async Task OrderBookPrice(InstrumentBidAskPair quote, bool isEod)
        {
            throw new System.NotImplementedException();
        }

        public async Task AccountMarginEvent(MarginEventMessage eventMessage)
        {
            throw new System.NotImplementedException();
        }

        public async Task UpdateAccountStats(AccountStatsUpdateMessage message)
        {
            throw new System.NotImplementedException();
        }

        public async Task NewTrade(TradeContract trade)
        {
            throw new System.NotImplementedException();
        }

        public async Task ExternalOrder(ExecutionReport trade)
        {
            throw new System.NotImplementedException();
        }

        public async Task PositionHistory(PositionHistoryEvent historyEvent)
        {
            _container.Add(historyEvent);
        }

        public async Task RfqChanged(RfqChangedEvent rfqChangedEvent)
        {
            throw new NotImplementedException();
        }
    }
}