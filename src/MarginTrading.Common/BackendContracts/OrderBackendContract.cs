using System;
using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class OrderBackendContract
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public string Instrument { get; set; }
        public OrderDirection Type { get; set; }
        public OrderStatus Status { get; set; }
        public OrderCloseReason CloseReason { get; set; }
        public OrderRejectReason RejectReason { get; set; }
        public string RejectReasonText { get; set; }
        public double? ExpectedOpenPrice { get; set; }
        public double OpenPrice { get; set; }
        public double ClosePrice { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public double Volume { get; set; }
        public double MatchedVolume { get; set; }
        public double MatchedCloseVolume { get; set; }
        public double? TakeProfit { get; set; }
        public double? StopLoss { get; set; }
        public double? Fpl { get; set; }
        public double OpenCommission { get; set; }
        public double CloseCommission { get; set; }
        public double SwapCommission { get; set; }

    }
}
