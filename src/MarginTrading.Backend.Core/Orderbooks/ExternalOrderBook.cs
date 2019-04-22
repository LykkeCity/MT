using System;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Orderbooks
{
    public class ExternalOrderBook
    {
        public ExternalOrderBook(string exchangeName, string assetPairId, DateTime timestamp, VolumePrice[] asks, VolumePrice[] bids)
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

        public VolumePrice[] Asks { get; set; }

        public VolumePrice[] Bids { get; set; }

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
                Bid = Bids[0].Price,
                Ask = Asks[0].Price,
                Date = Timestamp,
                Instrument = AssetPairId
            };
        }

        public decimal? GetMatchedPrice(decimal volumeToMatch, OrderDirection orderTypeToMatch)
        {
            volumeToMatch = Math.Abs(volumeToMatch);
            
            if (volumeToMatch == 0)
                return null;

            var source = orderTypeToMatch == OrderDirection.Buy ? Asks : Bids;
            
            var leftVolumeToMatch = volumeToMatch;

            decimal matched = 0;
            
            foreach (var priceLevel in source)
            {
                var matchedVolume = Math.Min(priceLevel.Volume, leftVolumeToMatch);

                matched += priceLevel.Price * matchedVolume;
                
                leftVolumeToMatch = Math.Round(leftVolumeToMatch - matchedVolume, MarginTradingHelpers.VolumeAccuracy);
                
                if (leftVolumeToMatch <= 0)
                    break;
            }

            //order is not fully matched
            if (leftVolumeToMatch > 0)
                return null;

            return matched / volumeToMatch;
        }
    }
}