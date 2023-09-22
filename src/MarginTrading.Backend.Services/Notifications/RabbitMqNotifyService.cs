// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.ExchangeConnector;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using MarginTrading.Contract.RabbitMqMessageModels;
using MarginTrading.Backend.Services.Mappers;

namespace MarginTrading.Backend.Services.Notifications
{
    public class RabbitMqNotifyService : IRabbitMqNotifyService
    {
        private readonly IDateService _dateService;
        private readonly MarginTradingSettings _settings;
        private readonly ILog _log;
        private readonly IOrderReader _orderReader;
        private readonly IRabbitMqProducerContainer _producerContainer;

        public RabbitMqNotifyService(IDateService dateService,
            MarginTradingSettings settings,
            ILog log,
            IOrderReader orderReader,
            IRabbitMqProducerContainer rabbitMqProducerContainer)
        {
            _dateService = dateService;
            _settings = settings;
            _log = log;
            _orderReader = orderReader;
            _producerContainer = rabbitMqProducerContainer;

            RegisterPublishers();
        }

        public Task OrderHistory(Order order, OrderUpdateType orderUpdateType, string activitiesMetadata = null)
        {
            var relatedOrders = new List<Order>();

            foreach (var relatedOrderInfo in order.RelatedOrders)
            {
                if (_orderReader.TryGetOrderById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrders.Add(relatedOrder);
                }
            }

            var historyEvent = new OrderHistoryEvent
            {
                OrderSnapshot = order.ConvertToContract(relatedOrders),
                Timestamp = _dateService.Now(),
                Type = orderUpdateType.ToType<OrderHistoryTypeContract>(),
                ActivitiesMetadata = activitiesMetadata
            };

            return TryProduceMessageAsync(historyEvent);
        }

        public Task OrderBookPrice(InstrumentBidAskPair quote, bool isEod)
        {
            return TryProduceMessageAsync(quote.ToRabbitMqContract(isEod));
        }

        public Task AccountMarginEvent(MarginEventMessage eventMessage)
        {
            return TryProduceMessageAsync(eventMessage);
        }

        public Task UpdateAccountStats(AccountStatsUpdateMessage message)
        {
            return TryProduceMessageAsync(message);
        }

        public Task NewTrade(TradeContract trade)
        {
            return TryProduceMessageAsync(trade);
        }

        public Task ExternalOrder(ExecutionReport trade)
        {
            return TryProduceMessageAsync(trade);
        }

        public Task PositionHistory(PositionHistoryEvent historyEvent)
        {
            return TryProduceMessageAsync(historyEvent);
        }

        public Task Rfq(RfqEvent rfqEvent) =>
            TryProduceMessageAsync(rfqEvent);

        private async Task TryProduceMessageAsync<T>(T message)
        {
            string exchangeName = null;

            try
            {
                var (publisherInfo, producer) = _producerContainer.GetProducer<T>();
                exchangeName = publisherInfo.ExchangeName;
                await producer.ProduceAsync(message);

                if (publisherInfo.LogEventPublishing && publisherInfo.LoggingStrategy.CanLog())
                {
                    var messageStr =  message.ToJson();
                    await _log.WriteInfoAsync(nameof(RabbitMqNotifyService), exchangeName, messageStr,
                        "Published RabbitMqEvent");
                }
            }
            catch (Exception ex)
            {
#pragma warning disable 4014
                var messageStr =  message.ToJson();
                _log.WriteErrorAsync(nameof(RabbitMqNotifyService), exchangeName, messageStr, ex);
#pragma warning restore 4014
            }
        }

        private void RegisterPublishers()
        {
            _producerContainer.RegisterProducer<OrderHistoryEvent>(_settings.RabbitMqPublishers.OrderHistory);
            _producerContainer.RegisterProducer<BidAskPairRabbitMqContract>(
                _settings.RabbitMqPublishers.OrderbookPrices);
            _producerContainer.RegisterProducer<MarginEventMessage>(_settings.RabbitMqPublishers.AccountMarginEvents);
            _producerContainer.RegisterProducer<AccountStatsUpdateMessage>(_settings.RabbitMqPublishers.AccountStats);
            _producerContainer.RegisterProducer<TradeContract>(_settings.RabbitMqPublishers.Trades);
            _producerContainer.RegisterProducer<PositionHistoryEvent>(_settings.RabbitMqPublishers.PositionHistory);
            _producerContainer.RegisterProducer<ExecutionReport>(_settings.RabbitMqPublishers.ExternalOrder);
            _producerContainer.RegisterProducer<RfqEvent>(_settings.RabbitMqPublishers.RfqChanged);
        }
    }
}