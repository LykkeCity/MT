using System;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Extensions;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    public class TradesConsumer:
        IEventConsumer<OrderPlacedEventArgs>,
        IEventConsumer<OrderClosedEventArgs>
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;

        public TradesConsumer(IRabbitMqNotifyService rabbitMqNotifyService)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
        }
        
        public void ConsumeEvent(object sender, OrderPlacedEventArgs ea)
        {
            if (ea.Order.IsOpened())
            {
                var tradeType = ea.Order.GetOrderType().ToType<TradeType>();
                var trade = new TradeContract
                {
                    Id = ea.Order.Id + '_' + tradeType, // todo: fix ids?
                    AccountId = ea.Order.AccountId,
                    ClientId = ea.Order.ClientId,
                    OrderId = ea.Order.Id,
                    AssetPairId = ea.Order.Instrument,
                    Date = ea.Order.OpenDate.Value,
                    Price = ea.Order.OpenPrice,
                    Volume = ea.Order.MatchedOrders.SummaryVolume,
                    Type = tradeType
                };

                _rabbitMqNotifyService.NewTrade(trade);
            }
        }

        public void ConsumeEvent(object sender, OrderClosedEventArgs ea)
        {
            if (ea.Order.IsClosed())
            {
                var tradeType = ea.Order.GetCloseType().ToType<TradeType>();
                var trade = new TradeContract
                {
                    Id = ea.Order.Id + '_' + tradeType, // todo: fix ids?,
                    AccountId = ea.Order.AccountId,
                    ClientId = ea.Order.ClientId,
                    OrderId = ea.Order.Id,
                    AssetPairId = ea.Order.Instrument,
                    Date = ea.Order.CloseDate.Value,
                    Price = ea.Order.ClosePrice,
                    Volume = ea.Order.MatchedCloseOrders.SummaryVolume,
                    Type = tradeType
                };

                _rabbitMqNotifyService.NewTrade(trade);
            }
        }

        public int ConsumerRank => 101;
    }
}