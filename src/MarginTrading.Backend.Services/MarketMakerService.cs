using System;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MarketMakerFeed;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Services
{
    public class MarketMakerService : IFeedConsumer
    {
        private readonly IMarketMakerMatchingEngine _matchingEngine;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private readonly IMaintenanceModeService _maintenanceModeService;

        public MarketMakerService(IMarketMakerMatchingEngine matchingEngine, 
            IAssetPairDayOffService assetPairDayOffService,
            IMaintenanceModeService maintenanceModeService)
        {
            _matchingEngine = matchingEngine;
            _assetPairDayOffService = assetPairDayOffService;
            _maintenanceModeService = maintenanceModeService;
        }

        public void ConsumeFeed(MarketMakerOrderCommandsBatchMessage batch)
        {
            batch.AssetPairId.RequiredNotNullOrWhiteSpace(nameof(batch.AssetPairId));
            batch.Commands.RequiredNotNull(nameof(batch.Commands));

            if (_maintenanceModeService.CheckIsEnabled())
                return;
            
            if (_assetPairDayOffService.IsDayOff(batch.AssetPairId))
                return;

            var model = new SetOrderModel {MarketMakerId = batch.MarketMakerId};

            ConvertCommandsToOrders(batch, model);
            if (model.OrdersToAdd?.Count > 0 || model.DeleteByInstrumentsBuy?.Count > 0 ||
                model.DeleteByInstrumentsSell?.Count > 0)
            {
                _matchingEngine.SetOrders(model);
            }
        }

        private void ConvertCommandsToOrders(MarketMakerOrderCommandsBatchMessage batch, SetOrderModel model)
        {
            var setCommands = batch.Commands.Where(c => c.CommandType == MarketMakerOrderCommandType.SetOrder && c.Direction != null).ToList();
            model.OrdersToAdd = setCommands
                .Select(c => new LimitOrder
                {
                    Id = Guid.NewGuid().ToString("N"),
                    MarketMakerId = batch.MarketMakerId,
                    CreateDate = DateTime.UtcNow,
                    Instrument = batch.AssetPairId,
                    Price = c.Price.RequiredNotNull(nameof(c.Price)),
                    Volume = c.Direction.RequiredNotNull(nameof(c.Direction)) == OrderDirection.Buy
                        ? c.Volume.Value
                        : -c.Volume.Value
                }).ToList();

            AddOrdersToDelete(batch, model);
        }

        private static void AddOrdersToDelete(MarketMakerOrderCommandsBatchMessage batch, SetOrderModel model)
        {
            var directions = batch.Commands.Where(c => c.CommandType == MarketMakerOrderCommandType.DeleteOrder).Select(c => c.Direction).Distinct().ToList();
            model.DeleteByInstrumentsBuy = directions.Where(d => d == OrderDirection.Buy || d == null).Select(d => batch.AssetPairId).ToList();
            model.DeleteByInstrumentsSell = directions.Where(d => d == OrderDirection.Sell || d == null).Select(d => batch.AssetPairId).ToList();
        }
    }
}