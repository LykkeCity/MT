using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class OutliersOrderbooksService : IOutliersOrderbooksService
    {
        private readonly IBestPricesService _bestPricesService;
        private readonly IPriceCalcSettingsService _priceCalcSettingsService;

        public OutliersOrderbooksService(
            IBestPricesService bestPricesService,
            IPriceCalcSettingsService priceCalcSettingsService)
        {
            _bestPricesService = bestPricesService;
            _priceCalcSettingsService = priceCalcSettingsService;
        }

        public IReadOnlyList<ExternalOrderbook> FindOutliers(string assetPairId, ImmutableDictionary<string, ExternalOrderbook> validOrderbooks)
        {
            var bestPrices = validOrderbooks.Values
                .Select(o => (Orderbook: o, BestPrices: _bestPricesService.Calc(o)))
                .ToList();

            var result = new List<ExternalOrderbook>();
            var medianBid = GetMedian(bestPrices.Select(p => p.BestPrices.BestBid), true);
            var medianAsk = GetMedian(bestPrices.Select(p => p.BestPrices.BestAsk), false);
            var threshold = GetOutlierThreshold(assetPairId);
            foreach (var (orderbook, prices) in bestPrices)
            {
                if (Math.Abs(prices.BestBid - medianBid) > threshold * medianBid)
                    result.Add(orderbook);
                else if (Math.Abs(prices.BestAsk - medianAsk) > threshold * medianAsk)
                    result.Add(orderbook);
            }

            return result;
        }

        private decimal GetOutlierThreshold(string assetPairId)
        {
            return _priceCalcSettingsService.GetOutlierThreshold(assetPairId);
        }

        private static decimal GetMedian(IEnumerable<decimal> src, bool onEvenCountGetLesser)
        {
            var sorted = src.OrderBy(e => e).ToList();
            int mid = sorted.Count / 2;
            if (sorted.Count % 2 != 0)
                return sorted[mid];
            else if (onEvenCountGetLesser)
                return sorted[mid - 1];
            else
                return sorted[mid];
        }
    }
}