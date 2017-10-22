using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class ArbitrageFreeSpreadService : IArbitrageFreeSpreadService
    {
        private readonly IPriceCalcSettingsService _priceCalcSettingsService;

        public ArbitrageFreeSpreadService(IPriceCalcSettingsService priceCalcSettingsService)
        {
            _priceCalcSettingsService = priceCalcSettingsService;
        }

        public Orderbook Transform(ExternalOrderbook primaryOrderbook, IReadOnlyDictionary<string, BestPrices> bestPrices)
        {
            var arbitrageFreeSpread = GetArbitrageFreeSpread(bestPrices);
            var primaryBestPrices = bestPrices[primaryOrderbook.ExchangeName];
            var bidShift = primaryBestPrices.BestBid - arbitrageFreeSpread.WorstBid;
            var askShift = arbitrageFreeSpread.WorstAsk - primaryBestPrices.BestAsk;
            var volumeMultiplier = _priceCalcSettingsService.GetVolumeMultiplier(primaryOrderbook.AssetPairId, primaryOrderbook.ExchangeName);
            return new Orderbook(
                primaryOrderbook.Bids.Select(b => new OrderbookPosition(b.Price - bidShift, b.Volume * volumeMultiplier)).ToImmutableArray(),
                primaryOrderbook.Asks.Select(b => new OrderbookPosition(b.Price + askShift, b.Volume * volumeMultiplier)).ToImmutableArray());
        }

        private static (decimal WorstBid, decimal WorstAsk) GetArbitrageFreeSpread(IReadOnlyDictionary<string, BestPrices> bestPrices)
        {
            var worstBid = bestPrices.Values.Min(p => p.BestBid);
            var worstAsk = bestPrices.Values.Max(p => p.BestAsk);
            if (worstBid == worstAsk)
            {
                worstBid -= 0.00000001m; // hello crutches
            }

            return (worstBid, worstAsk);
        }
    }
}