// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Extensions;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    /// <summary>
    /// Consumes core internal event <see cref="OrderExecutedEventArgs"/> and
    /// publishes <see cref="TradeContract"/> to RabbitMq
    /// </summary>
    public class TradesConsumer: IEventConsumer<OrderExecutedEventArgs>
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly ObsoleteFeature _compiledSchedulePublishingFeature;

        public TradesConsumer(IRabbitMqNotifyService rabbitMqNotifyService, 
            [CanBeNull] ObsoleteFeature compiledSchedulePublishingFeature)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _compiledSchedulePublishingFeature = compiledSchedulePublishingFeature ?? ObsoleteFeature.Default;
        }

        public void ConsumeEvent(object sender, OrderExecutedEventArgs ea)
        {
            if (!_compiledSchedulePublishingFeature.IsEnabled)
                return;

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