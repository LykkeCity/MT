// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Extensions;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    //TODO: change events and models
    public class TradesConsumer:
        IEventConsumer<OrderExecutedEventArgs>
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;

        public TradesConsumer(IRabbitMqNotifyService rabbitMqNotifyService)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
        }

        public void ConsumeEvent(object sender, OrderExecutedEventArgs ea)
        {
            var tradeType = ea.Order.Direction.ToType<TradeType>();
            var volume = Math.Abs(ea.Order.Volume);
            
            var trade = new TradeContract
            {
                Id = ea.Order.Id,
                AccountId = ea.Order.AccountId,
                OrderId = ea.Order.Id,
                AssetPairId = ea.Order.AssetPairId,
                Date = ea.Order.Executed.Value,
                Price = ea.Order.ExecutionPrice.Value,
                Volume = volume,
                Type = tradeType
            };

            _rabbitMqNotifyService.NewTrade(trade);
        }

        public int ConsumerRank => 101;
    }
}