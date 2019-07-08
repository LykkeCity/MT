// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Contract.BackendContracts
{
    public class OrderFullContract : OrderContract
    {
        public string TradingConditionId { get; set; }
        public int AssetAccuracy { get; set; }
        public decimal QuoteRate { get; set; }
        public DateTime? StartClosingDate { get; set; }
        public OrderFillTypeContract FillType { get; set; }
        public string Comment { get; set; }
        public decimal InterestRateSwap { get; set; }
        public decimal MarginRate { get; set; }
        public decimal MarginInit { get; set; }
        public decimal MarginMaintenance { get; set; }
        public DateTime UpdateTimestamp { get; set; }
        
        /// <summary>
        /// Business operation type which caused last change 
        /// </summary>
        public OrderUpdateTypeContract OrderUpdateType { get; set; }

    }
}
