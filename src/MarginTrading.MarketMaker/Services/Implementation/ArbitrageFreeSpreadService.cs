using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Models;
using MoreLinq;

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
            var isArbitrageFreeSpreadEnabled = _priceCalcSettingsService.IsStepEnabled(OrderbookGeneratorStepEnum.GetArbitrageFreeSpread,
                primaryOrderbook.AssetPairId);

            var arbitrageFreeSpread = isArbitrageFreeSpreadEnabled
                ? GetArbitrageFreeSpread(bestPrices)
                : GetArbitrageFreeSpread(
                    ImmutableDictionary.CreateRange(bestPrices.Where(p => p.Key == primaryOrderbook.ExchangeName)));
            var primaryBestPrices = bestPrices[primaryOrderbook.ExchangeName];
            var bidShift = arbitrageFreeSpread.WorstBid - primaryBestPrices.BestBid; // negative
            var askShift = arbitrageFreeSpread.WorstAsk - primaryBestPrices.BestAsk; // positive
            var volumeMultiplier = _priceCalcSettingsService.GetVolumeMultiplier(primaryOrderbook.AssetPairId, primaryOrderbook.ExchangeName);
            var priceMarkups = _priceCalcSettingsService.GetPriceMarkups(primaryOrderbook.AssetPairId);
            return new Orderbook(
                primaryOrderbook.Bids.Select(b => new OrderbookPosition(b.Price + bidShift + priceMarkups.Bid, b.Volume * volumeMultiplier)).ToImmutableArray(),
                primaryOrderbook.Asks.Select(b => new OrderbookPosition(b.Price + askShift + priceMarkups.Ask, b.Volume * volumeMultiplier)).ToImmutableArray());
        }

        private static (decimal WorstBid, decimal WorstAsk) GetArbitrageFreeSpread(IReadOnlyDictionary<string, BestPrices> bestPrices)
        {
            var worstBid = bestPrices.Values.Min(p => p.BestBid);
            var worstAsk = bestPrices.Values.Max(p => p.BestAsk);
            if (worstBid >= worstAsk)
            {
                worstBid = worstAsk - 0.00000001m; // hello crutches
            }

            return (worstBid, worstAsk);
        }
    }
}