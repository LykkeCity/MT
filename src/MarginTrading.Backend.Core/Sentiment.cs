// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.Extensions;

namespace MarginTrading.Backend.Core
{
    public readonly struct Sentiment
    {
        private readonly long _shortPositionsCounter;

        private readonly long _longPositionsCounter;
        
        public readonly string ProductId;

        /// <summary>
        /// Creates sentiment for product
        /// </summary>
        /// <param name="productId">The product identifier</param>
        /// <param name="shortCounter">The number of short positions by product</param>
        /// <param name="longCounter">The number of long positions by product</param>
        public Sentiment(string productId, long? shortCounter = null, long? longCounter = null)
        {
            ProductId = productId;
            _shortPositionsCounter = shortCounter.ZeroIfNegative();
            _longPositionsCounter = longCounter.ZeroIfNegative();
        }
        
        public void Deconstruct(out decimal shortShare, out decimal longShare) =>
            (shortShare, longShare) = CalculateShares();

        public Sentiment AddShort(long n = 1) => new Sentiment(ProductId, _shortPositionsCounter + n, _longPositionsCounter);
        
        public Sentiment AddLong(long n = 1) => new Sentiment(ProductId, _shortPositionsCounter, _longPositionsCounter + n);
        
        public Sentiment RemoveShort(long n = 1) => new Sentiment(ProductId, _shortPositionsCounter - n, _longPositionsCounter);
        
        public Sentiment RemoveLong(long n = 1) => new Sentiment(ProductId, _shortPositionsCounter, _longPositionsCounter - n);
        

        private (decimal, decimal) CalculateShares()
        {
            var totalPositions = _longPositionsCounter + _shortPositionsCounter;

            decimal longShare = Math.Round(_longPositionsCounter * 100 / (decimal)totalPositions, decimals: 2);
            
            decimal shortShare = Math.Round(100 - longShare, decimals: 2);

            return (shortShare, longShare);
        }
    }
}