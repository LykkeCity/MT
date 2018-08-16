using System;
using System.Collections.Generic;
using MarginTrading.Backend.Contracts.Orders;

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
        /// Instrument id (e.g."BTCUSD", where BTC - base asset unit, USD - quoting unit)
        /// </summary>
        public string AssetPairId { get; set; }
        
        /// <summary>
        /// When the position was opened
        /// </summary>
        public DateTime OpenTimestamp { get; set; }

        /// <summary>
        /// The direction of the position (Long or Short)
        /// </summary>
        public PositionDirectionContract Direction { get; set; }

        /// <summary>
        /// Open price (in quoting asset units per one base unit)
        /// </summary>
        public decimal OpenPrice { get; set; }
        
        /// <summary>
        /// Opening FX Rate
        /// </summary>
        public decimal OpenFxPrice { get; set; }

        /// <summary>
        /// Expected open price (in quoting asset units per one base unit)
        /// </summary>
        public decimal? ExpectedOpenPrice { get; set; }

        /// <summary>
        /// Current price for closing of position (in quoting asset units per one base unit)
        /// </summary>
        public decimal ClosePrice { get; set; }

        /// <summary>
        /// Current position volume in base asset units
        /// </summary>
        public decimal CurrentVolume { get; set; } // TODO: RENAME TO 'Volume'

        /// <summary>
        /// Profit and loss of the position in account asset units (without commissions)
        /// </summary>
        public decimal PnL { get; set; }
        
        /// <summary>
        /// PnL changed on account balance
        /// </summary>
        public decimal ChargedPnl { get; set; }

        /// <summary>
        /// Current margin value in account asset units
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
        /// The related order Ids (sl, tp orders) 
        /// </summary>
        public List<string> RelatedOrders { get; set; }
        
        /// <summary>
        /// The related orders (sl, tp orders) 
        /// </summary>
        public List<RelatedOrderInfoContract> RelatedOrderInfos { get; set; }
    }
}