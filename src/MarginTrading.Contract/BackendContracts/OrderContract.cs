using System;
using System.Collections.Generic;

namespace MarginTrading.Contract.BackendContracts
{
    public class OrderContract
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public string AccountAssetId { get; set; }
        public string Instrument { get; set; }
        public OrderDirectionContract Type { get; set; }
        public OrderStatusContract Status { get; set; }
        public OrderCloseReasonContract CloseReason { get; set; }
        public OrderRejectReasonContract RejectReason { get; set; }
        public string RejectReasonText { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public decimal Volume { get; set; }
        public decimal MatchedVolume { get; set; }
        public decimal MatchedCloseVolume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        /// <summary> Floating profit </summary>
        public decimal Fpl { get; set; }
        /// <summary> Total profit </summary>
        /// <remarks> Fpl + comissions </remarks>
        public decimal PnL { get; set; }
        public decimal CommissionLot { get; set; }
        public decimal OpenCommission { get; set; }
        public decimal CloseCommission { get; set; }
        public decimal SwapCommission { get; set; }
        public string EquivalentAsset { get; set; }
        public decimal OpenPriceEquivalent { get; set; }
        public decimal ClosePriceEquivalent { get; set; }
        public List<MatchedOrderBackendContract> MatchedOrders { get; set; } = new List<MatchedOrderBackendContract>();
        public List<MatchedOrderBackendContract> MatchedCloseOrders { get; set; } = new List<MatchedOrderBackendContract>();
    }
}