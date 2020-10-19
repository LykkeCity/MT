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
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.Backend.Services.Notifications
{
    public class RabbitMqNotifyService : IRabbitMqNotifyService
    {
        private readonly IDateService _dateService;
        private readonly MarginTradingSettings _settings;
        private readonly Dictionary<string, IMessageProducer<string>> _publishers;
        private readonly ILog _log;
        private readonly IOrderReader _orderReader;

        public RabbitMqNotifyService(IDateService dateService, 
            MarginTradingSettings settings,
            ILog log, 
            IOrderReader orderReader,
            IRabbitMqService rabbitMqService)
        {
            _dateService = dateService;
            _settings = settings;
            _log = log;
            _orderReader = orderReader;
            _publishers = new Dictionary<string, IMessageProducer<string>>();

            RegisterPublishers(rabbitMqService);
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
            
            return TryProduceMessageAsync(_settings.RabbitMqQueues.OrderHistory.ExchangeName, historyEvent,
                _settings.RabbitMqQueues.OrderHistory.LogEventPublishing);
        }

        public Task OrderBookPrice(InstrumentBidAskPair quote, bool isEod)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.OrderbookPrices.ExchangeName,
                quote.ToRabbitMqContract(isEod),
                false);
        }

        public Task AccountMarginEvent(MarginEventMessage eventMessage)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.AccountMarginEvents.ExchangeName, eventMessage,
                _settings.RabbitMqQueues.AccountMarginEvents.LogEventPublishing);
        }

        public Task UpdateAccountStats(AccountStatsUpdateMessage message)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.AccountStats.ExchangeName, message,
                _settings.RabbitMqQueues.AccountStats.LogEventPublishing);
        }

        public Task NewTrade(TradeContract trade)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.Trades.ExchangeName, trade,
                _settings.RabbitMqQueues.Trades.LogEventPublishing);
        }

        public Task ExternalOrder(ExecutionReport trade)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.ExternalOrder.ExchangeName, trade,
                _settings.RabbitMqQueues.ExternalOrder.LogEventPublishing);
        }
        
        public Task PositionHistory(PositionHistoryEvent historyEvent)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.PositionHistory.ExchangeName, historyEvent,
                _settings.RabbitMqQueues.PositionHistory.LogEventPublishing);
        }

        private async Task TryProduceMessageAsync(string exchangeName, object message, bool logEvent)
        {
            string messageStr = null;
            try
            {
                messageStr = message.ToJson();
                await _publishers[exchangeName].ProduceAsync(messageStr);

                if (logEvent)
                    _log.WriteInfoAsync(nameof(RabbitMqNotifyService), exchangeName, messageStr,
                        "Published RabbitMqEvent");
            }
            catch (Exception ex)
            {
#pragma warning disable 4014
                _log.WriteErrorAsync(nameof(RabbitMqNotifyService), exchangeName, messageStr, ex);
#pragma warning restore 4014
            }
        }

        private void RegisterPublishers(IRabbitMqService rabbitMqService)
        {
            var publishExchanges = new List<string>
            {
                _settings.RabbitMqQueues.OrderHistory.ExchangeName,
                _settings.RabbitMqQueues.OrderbookPrices.ExchangeName,
                _settings.RabbitMqQueues.AccountMarginEvents.ExchangeName,
                _settings.RabbitMqQueues.AccountStats.ExchangeName,
                _settings.RabbitMqQueues.Trades.ExchangeName,
                _settings.RabbitMqQueues.PositionHistory.ExchangeName,
                _settings.RabbitMqQueues.ExternalOrder.ExchangeName,
            };

            var bytesSerializer = new BytesStringSerializer();

            foreach (var exchangeName in publishExchanges)
            {
                var settings = new RabbitMqSettings
                {
                    ConnectionString = _settings.MtRabbitMqConnString, ExchangeName
                        = exchangeName
                };
                _publishers[exchangeName] = rabbitMqService.GetProducer(settings, bytesSerializer);
            }
        }
    }
}