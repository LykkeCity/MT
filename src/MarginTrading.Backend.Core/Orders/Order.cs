using System;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Core.Orders
{
    public class Order : IOrder
    {
        public string Id { get; set; }
        public long Code { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountAssetId { get; set; }
        public string OpenOrderbookId { get; set; }
        public string CloseOrderbookId { get; set; }
        public string Instrument { get; set; }
        public string MarginCalcInstrument { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal QuoteRate { get; set; }
        public int AssetAccuracy { get; set; }
        public decimal Volume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal OpenCommission { get; set; }
        public decimal CloseCommission { get; set; }
        public decimal CommissionLot { get; set; }
        public decimal SwapCommission { get; set; }
        public string EquivalentAsset { get; set; }
        public decimal OpenPriceEquivalent { get; set; }
        public decimal ClosePriceEquivalent { get; set; }
        public string OpenExternalOrderId { get; set; }
        public string OpenExternalProviderId { get; set; }
        public string CloseExternalOrderId { get; set; }
        public string CloseExternalProviderId { get; set; }
        public DateTime? StartClosingDate { get; set; }
        public OrderStatus Status { get; set; }
        public OrderCloseReason CloseReason { get; set; }
        public OrderFillType FillType { get; set; }
        public OrderRejectReason RejectReason { get; set; }
        public string CloseRejectReasonText { get; set; }
        public string RejectReasonText { get; set; }
        public string Comment { get; set; }
        public MatchedOrderCollection MatchedOrders { get; set; } = new MatchedOrderCollection();
        public MatchedOrderCollection MatchedCloseOrders { get; set; } = new MatchedOrderCollection();
        public MatchingEngineMode MatchingEngineMode { get; set; } = MatchingEngineMode.MarketMaker;
        public string LegalEntity { get; set; }  
        public FplData FplData { get; set; } = new FplData();
        public bool ForceOpen { get; set; }
        public OrderType OrderType { get; set; }
        public string ParentOrderId { get; set; }
        public string ParentPositionId { get; set; }
        public DateTime? Validity { get; set; }
        public OriginatorType Originator { get; set; }
    }
}