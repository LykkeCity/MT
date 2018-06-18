using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Core.Orders
{
    public interface IOrderHistory
    {
        string Id { get; }
        long Code { get; }
        string AccountId { get; }
        string TradingConditionId { get; }
        string AccountAssetId { get; }
        string Instrument { get; }
        OrderDirection Type { get; }
        DateTime CreateDate { get; }
        DateTime? OpenDate { get; }
        DateTime? CloseDate { get; }
        decimal? ExpectedOpenPrice { get; }
        decimal OpenPrice { get; }
        decimal ClosePrice { get; }
        decimal QuoteRate { get; }
        int AssetAccuracy { get; }
        decimal Volume { get; }
        decimal? TakeProfit { get; }
        decimal? StopLoss { get; }
        decimal CommissionLot { get; }
        decimal OpenCommission { get; }
        decimal CloseCommission { get; }
        decimal SwapCommission { get; }
        string EquivalentAsset { get; }
        decimal OpenPriceEquivalent { get; }
        decimal ClosePriceEquivalent { get; }
        DateTime? StartClosingDate { get; }
        OrderStatus Status { get; }
        OrderCloseReason CloseReason { get; }
        OrderFillType FillType { get; }
        OrderRejectReason RejectReason { get; }
        string RejectReasonText { get; }
        string Comment { get; }
        List<MatchedOrder> MatchedOrders { get; }
        List<MatchedOrder> MatchedCloseOrders { get; }

        decimal MatchedVolume { get; }
        decimal MatchedCloseVolume { get; }
        decimal Fpl { get; }
        decimal PnL { get; }
        decimal InterestRateSwap { get; }
        decimal MarginInit { get; }
        decimal MarginMaintenance { get; }

        OrderUpdateType OrderUpdateType { get; }

        string OpenExternalOrderId { get; }
        string OpenExternalProviderId { get; }
        string CloseExternalOrderId { get; }
        string CloseExternalProviderId { get; }

        MatchingEngineMode MatchingEngineMode { get; }
        string LegalEntity { get; }
        DateTime UpdateTimestamp { get; }

        [CanBeNull]
        string ParentPositionId { get; }

        [CanBeNull]
        string ParentOrderId { get; }
    }
}