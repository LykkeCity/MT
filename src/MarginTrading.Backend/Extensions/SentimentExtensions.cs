// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.Sentiments;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Extensions
{
    public static class SentimentExtensions
    {
        public static SentimentInfoContract ToContract(this Sentiment source)
        {
            var (shortShare, longShare) = source;

            return new SentimentInfoContract
            {
                InstrumentId = source.ProductId,
                Sell = shortShare,
                Buy = longShare
            };
        }
    }
}