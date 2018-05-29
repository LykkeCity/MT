using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Contracts.Positions
{
    /// <summary>
    /// Info about an open position
    /// </summary>
    public class OpenPositionContract
    {
        /// <summary>
        /// Position id
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Account id
        /// </summary>
        public string AccountId { get; set; }
        
        /// <summary>
        /// Asset pair id
        /// </summary>
        public string AssetPairId { get; set; }
        
        /// <summary>
        /// When the position was opened
        /// </summary>
        public DateTime OpenTimestamp { get; set; }

        /// <summary>
        /// The direction of the position
        /// </summary>
        public PositionDirectionContract Direction { get; set; }
        
        /// <summary>
        /// Open price
        /// </summary>
        public decimal OpenPrice { get; set; }
        
        /// <summary>
        /// Expected open price
        /// </summary>
        public decimal? ExpectedOpenPrice { get; set; }
        
        /// <summary>
        /// Current price for closing of position
        /// </summary>
        public decimal ClosePrice { get; set; }
        
        /// <summary>
        /// Current position volume in quoting asset units
        /// </summary>
        public decimal CurrentVolume { get; set; }
        
        /// <summary>
        /// Profit and loss of the position in base asset units (without commissions)
        /// </summary>
        public decimal PnL { get; set; }
        
        /// <summary>
        /// Current margin value
        /// </summary>
        public decimal Margin { get; set; }
        
        /// <summary>
        /// Current FxRate
        /// </summary>
        public decimal FxRate { get; set; }

        /// <summary>
        /// The trade which opened the position
        /// </summary>
        public string TradeId { get; set; }
        
        /// <summary>
        /// The related orders (sl, tp orders) 
        /// </summary>
        public List<string> RelatedOrders { get; set; }
    }
}