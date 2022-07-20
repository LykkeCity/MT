// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Sentiments
{
    /// <summary>
    /// Information on product sentiment
    /// </summary>
    [PublicAPI]
    public class SentimentInfoContract
    {
        /// <summary>
        /// Instrument identifier
        /// </summary>
        public string InstrumentId { get; set; }
        
        /// <summary>
        /// Sell share
        /// </summary>
        public decimal Sell { get; set; }
        
        /// <summary>
        /// Buy share
        /// </summary>
        public decimal Buy { get; set; }
    }
}