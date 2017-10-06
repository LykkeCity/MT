using System;
using System.Collections.Generic;
using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class OrderFullContract : OrderContract
    {
        public string TradingConditionId { get; set; }
        public int AssetAccuracy { get; set; }
        public decimal QuoteRate { get; set; }
        public DateTime? StartClosingDate { get; set; }
        public OrderFillType FillType { get; set; }
        public string Comment { get; set; }
        public decimal InterestRateSwap { get; set; }
        public decimal MarginInit { get; set; }
        public decimal MarginMaintenance { get; set; }
        public decimal OpenCrossPrice { get; set; }
        public decimal CloseCrossPrice { get; set; }
    }
}
