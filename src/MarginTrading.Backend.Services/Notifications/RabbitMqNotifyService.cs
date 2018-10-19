using System;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Common;
using Common.Log;
using Lykke.Service.ExchangeConnector.Client.Models;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Mappers;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.Backend.Services.Notifications
{
    public class RabbitMqNotifyService : IRabbitMqNotifyService
    {
        private readonly IDateService _dateService;
        private readonly MarginTradingSettings _settings;
        private readonly IIndex<string, IMessageProducer<string>> _publishers;
        private readonly ILog _log;

        public RabbitMqNotifyService(IDateService dateService, MarginTradingSettings settings,
            IIndex<string, IMessageProducer<string>> publishers, ILog log)
        {
            _dateService = dateService;
            _settings = settings;
            _publishers = publishers;
            _log = log;
        }

        public Task OrderHistory(Order order, OrderUpdateType orderUpdateType)
        {
            var historyEvent = new OrderHistoryEvent
            {
                OrderSnapshot = order.ConvertToContract(),
                Timestamp = _dateService.Now(),
                Type = orderUpdateType.ToType<OrderHistoryTypeContract>()
            };
            
            return TryProduceMessageAsync(_settings.RabbitMqQueues.OrderHistory.ExchangeName, historyEvent);
        }

        public Task OrderBookPrice(InstrumentBidAskPair quote)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.OrderbookPrices.ExchangeName,
                quote.ToRabbitMqContract());
        }

        public Task AccountUpdated(IMarginTradingAccount account)
        {
            return AccountChanged(account, AccountEventTypeEnum.Updated);
        }

        private Task AccountChanged(IMarginTradingAccount account, AccountEventTypeEnum eventType)
        {
            var message = new AccountChangedMessage
            {
                Account = account.ToFullBackendContract(_settings.IsLive),
                EventType = eventType,
            };

            return TryProduceMessageAsync(_settings.RabbitMqQueues.AccountChanged.ExchangeName, message);
        }

        public Task AccountMarginEvent(MarginEventMessage eventMessage)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.AccountMarginEvents.ExchangeName, eventMessage);
        }

        public Task UpdateAccountStats(AccountStatsUpdateMessage message)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.AccountStats.ExchangeName, message);
        }

        public Task NewTrade(TradeContract trade)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.Trades.ExchangeName, trade);
        }

        public Task ExternalOrder(ExecutionReport trade)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.ExternalOrder.ExchangeName, trade);
        }
        
        public Task PositionHistory(PositionHistoryEvent historyEvent)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.PositionHistory.ExchangeName, historyEvent);
        }

        private async Task TryProduceMessageAsync(string exchangeName, object message)
        {
            string messageStr = null;
            try
            {
                messageStr = message.ToJson();
                await _publishers[exchangeName].ProduceAsync(messageStr);
            }
            catch (Exception ex)
            {
#pragma warning disable 4014
                _log.WriteErrorAsync(nameof(RabbitMqNotifyService), exchangeName, messageStr, ex);
#pragma warning restore 4014
            }
        }

        public void Stop()
        {
            ((IStopable) _publishers[_settings.RabbitMqQueues.OrderHistory.ExchangeName]).Stop();
            ((IStopable) _publishers[_settings.RabbitMqQueues.OrderbookPrices.ExchangeName]).Stop();
            ((IStopable) _publishers[_settings.RabbitMqQueues.AccountChanged.ExchangeName]).Stop();
            ((IStopable) _publishers[_settings.RabbitMqQueues.AccountMarginEvents.ExchangeName]).Stop();
            ((IStopable) _publishers[_settings.RabbitMqQueues.AccountStats.ExchangeName]).Stop();
            ((IStopable) _publishers[_settings.RabbitMqQueues.Trades.ExchangeName]).Stop();
            ((IStopable) _publishers[_settings.RabbitMqQueues.PositionHistory.ExchangeName]).Stop();
            ((IStopable) _publishers[_settings.RabbitMqQueues.ExternalOrder.ExchangeName]).Stop();
        }
    }
}