using System;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Core
{
    public interface IOrder : IBaseOrder
    {
        string ClientId { get; }
        string AccountId { get; }
        string TradingConditionId { get; }
        string AccountAssetId { get; }
        
        //Matching Engine ID used for open
        string OpenOrderbookId { get; }
        
        //Matching Engine ID used for close
        string CloseOrderbookId { get; }
        
        DateTime? OpenDate { get; }
        DateTime? CloseDate { get; }
        decimal? ExpectedOpenPrice { get; }
        decimal OpenPrice { get; }
        decimal ClosePrice { get; }
        decimal? TakeProfit { get; }
        decimal? StopLoss { get; }
        decimal OpenCommission { get; }
        decimal CloseCommission { get; }
        decimal CommissionLot { get; }
        decimal QuoteRate { get; }
        int AssetAccuracy { get; }
        DateTime? StartClosingDate { get; }
        OrderStatus Status { get; }
        OrderCloseReason CloseReason { get; }
        OrderFillType FillType { get; }
        OrderRejectReason RejectReason { get; }
        string CloseRejectReasonText { get; }
        string RejectReasonText { get; }
        string Comment { get; }
        MatchedOrderCollection MatchedCloseOrders { get; }
        decimal SwapCommission { get; }
        string EquivalentAsset { get; }
        decimal OpenPriceEquivalent { get; }
        decimal ClosePriceEquivalent { get; }
        
        #region Extenal orders matching
        
        string OpenExternalOrderId { get; }
        
        string OpenExternalProviderId { get; }
        
        string CloseExternalOrderId { get; }
        
        string CloseExternalProviderId { get; }
        
        MatchingEngineMode MatchingEngineMode { get; }
        
        string LegalEntity { get; set; }

        #endregion
    }

    public class Order : IOrder
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountAssetId { get; set; }
        public string OpenOrderbookId { get; set; }
        public string CloseOrderbookId { get; set; }
        public string Instrument { get; set; }
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
    }

    public enum OrderFillType
    {
        FillOrKill,
        PartialFill
    }

    public enum OrderDirection
    {
        Buy,
        Sell
    }

    public enum OrderStatus
    {
        WaitingForExecution,
        Active,
        Closed,
        Rejected,
        Closing
    }

    public enum OrderCloseReason
    {
        None,
        Close,
        StopLoss,
        TakeProfit,
        StopOut,
        Canceled,
        CanceledBySystem,
        ClosedByBroker
    }

    public enum OrderRejectReason
    {
        None,
        NoLiquidity,
        NotEnoughBalance,
        LeadToStopOut,
        AccountInvalidState,
        InvalidExpectedOpenPrice,
        InvalidVolume,
        InvalidTakeProfit,
        InvalidStoploss,
        InvalidInstrument,
        InvalidAccount,
        TradingConditionError,
        TechnicalError
    }

    public enum OrderUpdateType
    {
        Place,
        Cancel,
        Activate,
        Reject,
        Closing,
        Close,
        ChangeOrderLimits,
    }

    public static class OrderTypeExtension
    {
        public static OrderDirection GetOrderTypeToMatchInOrderBook(this OrderDirection orderType)
        {
            return orderType == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
        }
    }
}
