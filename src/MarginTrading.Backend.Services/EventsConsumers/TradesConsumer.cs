// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Extensions;
using MarginTrading.Contract.RabbitMqMessageModels;
using Microsoft.FeatureManagement;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    /// <summary>
    /// Consumes core internal event <see cref="OrderExecutedEventArgs"/> and
    /// publishes <see cref="TradeContract"/> to RabbitMq
    /// </summary>
    public class TradesConsumer: IEventConsumer<OrderExecutedEventArgs>
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IFeatureManager _featureManager;

        public TradesConsumer(IRabbitMqNotifyService rabbitMqNotifyService, 
            IFeatureManager featureManager)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _featureManager = featureManager;
        }

        public void ConsumeEvent(object sender, OrderExecutedEventArgs ea)
        {
            var publishingEnabled = _featureManager
                .IsEnabledAsync(Feature.TradeContractPublishing.ToString("G"))
                .GetAwaiter()
                .GetResult();
            if (!publishingEnabled)
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