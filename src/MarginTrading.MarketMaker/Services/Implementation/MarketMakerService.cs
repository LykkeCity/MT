using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices;
using MarginTrading.MarketMaker.Messages;
using MarginTrading.MarketMaker.Models;
using MarginTrading.MarketMaker.Settings;

namespace MarginTrading.MarketMaker.Services.Implemetation
{
    internal class MarketMakerService : IMarketMakerService
    {
        private const int OrdersVolume = 1000000;

        private readonly IAssetPairsSettingsService _assetPairsSettingsService;
        private readonly Lazy<IMessageProducer<OrderCommandsBatchMessage>> _messageProducer;
        private readonly ISystem _system;

        public MarketMakerService(IAssetPairsSettingsService assetPairsSettingsService, MarginTradingMarketMakerSettings marginTradingMarketMakerSettings,
            IRabbitMqService rabbitMqService, ISystem system)
        {
            _assetPairsSettingsService = assetPairsSettingsService;
            _system = system;
            _messageProducer = new Lazy<IMessageProducer<OrderCommandsBatchMessage>>(() =>
                CreateRabbitMqMessageProducer(marginTradingMarketMakerSettings, rabbitMqService));
        }

        public Task ProcessNewIcmBestBidAskAsync(BestBidAskMessage bestBidAsk)
        {
            if (bestBidAsk.Source != "ICM")
            {
                throw new InvalidOperationException("An invalid non-ICM message was received: " + bestBidAsk.ToJson());
            }

            var quotesSource = _assetPairsSettingsService.GetAssetPairQuotesSource(bestBidAsk.Asset);
            if (quotesSource != AssetPairQuotesSourceEnum.Icm || bestBidAsk.BestBid == null ||
                bestBidAsk.BestAsk == null)
            {
                return Task.CompletedTask;
            }

            return SendOrderCommandsAsync(bestBidAsk.Asset, bestBidAsk.BestBid.Value, bestBidAsk.BestAsk.Value);
        }

        public Task ProcessNewSpotOrderBookDataAsync(OrderBookMessage orderBookMessage)
        {
            var quotesSource = _assetPairsSettingsService.GetAssetPairQuotesSource(orderBookMessage.AssetPair);
            if (quotesSource == AssetPairQuotesSourceEnum.Spot || quotesSource == null)
            {
                var orderDirection = orderBookMessage.IsBuy ? OrderDirectionEnum.Buy : OrderDirectionEnum.Sell;
                return SendOrderCommandsAsync(orderBookMessage.AssetPair, orderDirection, orderBookMessage.BestPrice);
            }

            return Task.CompletedTask;
        }

        public async Task ProcessAssetPairSettingsAsync(AssetPairSettingsModel message)
        {
            AssetPairQuotesSourceEnum? quotesSource;
            if (message.SetNewQuotesSource != null)
            {
                quotesSource = message.SetNewQuotesSource.Value;
                await _assetPairsSettingsService.SetAssetPairQuotesSource(message.AssetPairId,
                    message.SetNewQuotesSource.Value);
            }
            else
            {
                quotesSource = _assetPairsSettingsService.GetAssetPairQuotesSource(message.AssetPairId);
            }

            if (quotesSource == AssetPairQuotesSourceEnum.Manual && message.PriceForBuyOrder != null &&
                message.PriceForSellOrder != null)
            {
                await SendOrderCommandsAsync(message.AssetPairId, message.PriceForSellOrder.Value,
                    message.PriceForBuyOrder.Value);
            }
        }

        private static IMessageProducer<OrderCommandsBatchMessage> CreateRabbitMqMessageProducer(
            MarginTradingMarketMakerSettings marginTradingMarketMakerSettings, IRabbitMqService rabbitMqService)
        {
            return rabbitMqService.CreateProducer<OrderCommandsBatchMessage>(
                marginTradingMarketMakerSettings.RabbitMq.OrderCommandsConnectionSettings, false);
        }

        private Task SendOrderCommandsAsync(string assetPairId, OrderDirectionEnum orderDirection, double price)
        {
            var commands = new[]
            {
                new OrderCommand {CommandType = OrderCommandTypeEnum.DeleteOrder},
                new OrderCommand
                {
                    CommandType = OrderCommandTypeEnum.SetOrder,
                    Direction = orderDirection,
                    Price = price,
                    Volume = OrdersVolume
                },
            };

            return SendOrderCommandsAsync(assetPairId, commands);
        }

        private Task SendOrderCommandsAsync(string assetPairId, double priceForSellOrder, double priceForBuyOrder)
        {
            var commands = new[]
            {
                new OrderCommand {CommandType = OrderCommandTypeEnum.DeleteOrder},
                new OrderCommand
                {
                    CommandType = OrderCommandTypeEnum.SetOrder,
                    Direction = OrderDirectionEnum.Sell,
                    Price = priceForSellOrder,
                    Volume = OrdersVolume
                },
                new OrderCommand
                {
                    CommandType = OrderCommandTypeEnum.SetOrder,
                    Direction = OrderDirectionEnum.Buy,
                    Price = priceForBuyOrder,
                    Volume = OrdersVolume
                },
            };

            return SendOrderCommandsAsync(assetPairId, commands);
        }

        private Task SendOrderCommandsAsync(string assetPairId, IReadOnlyList<OrderCommand> commands)
        {
            return _messageProducer.Value.ProduceAsync(new OrderCommandsBatchMessage
            {
                AssetPairId = assetPairId,
                Timestamp = _system.UtcNow,
                Commands = commands
            });
        }
    }
}