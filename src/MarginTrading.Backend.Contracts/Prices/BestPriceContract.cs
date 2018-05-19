using System;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Snow.Prices
{
    /// <summary>
    /// Info about best price
    /// </summary>
    [PublicAPI]
    public class BestPriceContract
    {
        /// <summary>
        /// Instrument
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Timestamp 
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Bid value
        /// </summary>
        public decimal Bid { get; set; }
        
        /// <summary>
        /// Ask value
        /// </summary>
        public decimal Ask { get; set; }
    }
}