using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Orderbooks
{
    public class ExternalOrderBook
    {
        public ExternalOrderBook(string exchangeName, string assetPairId, DateTime timestamp, List<VolumePrice> asks, List<VolumePrice> bids)
        {
            ExchangeName = exchangeName;
            AssetPairId = assetPairId;
            Timestamp = timestamp;
            Asks = asks;
            Bids = bids;
        }

        public string ExchangeName { get; private set; }

        public string AssetPairId { get; }

        public DateTime Timestamp { get; }

        public List<VolumePrice> Asks { get; }

        public List<VolumePrice> Bids { get; }

        public void ApplyExchangeIdFromSettings(string exchangeIdFromSettings)
        {
            if (!string.IsNullOrWhiteSpace(exchangeIdFromSettings))
            {
                ExchangeName = exchangeIdFromSettings;
            }
        }

        public InstrumentBidAskPair GetBestPrice()
        {
            return new InstrumentBidAskPair
            {
                Bid = Bids.First().Price,
                Ask = Asks.First().Price,
                Date = Timestamp,
                Instrument = AssetPairId
            };
        }

        public decimal? GetMatchedPrice(decimal volumeToMatch, OrderDirection orderTypeToMatch)
        {
            if (volumeToMatch == 0)
                return null;

            var source = orderTypeToMatch == OrderDirection.Buy ? Asks : Bids;
            
            var leftVolumeToMatch = Math.Abs(volumeToMatch);
            
            var matchedVolumePrices = new List<VolumePrice>();

            foreach (var priceLevel in source)
            {
                var matchedVolume = Math.Min(priceLevel.Volume, leftVolumeToMatch);

                matchedVolumePrices.Add(new VolumePrice
                {
                    Price = priceLevel.Price,
                    Volume = matchedVolume
                });

                leftVolumeToMatch = Math.Round(leftVolumeToMatch - matchedVolume, MarginTradingHelpers.VolumeAccuracy);
                
                if (leftVolumeToMatch <= 0)
                    break;
            }

            //order is not fully matched
            if (leftVolumeToMatch > 0)
                return null;

            return matchedVolumePrices.Sum(x => x.Price * Math.Abs(x.Volume)) / Math.Abs(volumeToMatch);
        }
    }
}