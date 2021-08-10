// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Core.Orders
{
    public class PositionHistory : IPositionHistory
    {
        public string Id { get; set; }
        public string DealId { get; set; }
        public long Code { get; set; }
        public string AssetPairId { get; set; }
        public PositionDirection Direction { get; set; }
        public decimal Volume { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountAssetId { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public string OpenMatchingEngineId { get; set; }
        public DateTime OpenDate { get; set; }
        public string OpenTradeId { get; set; }
        public OrderType OpenOrderType { get; set; }
        public decimal OpenOrderVolume { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal OpenFxPrice { get; set; }
        public string EquivalentAsset { get; set; }
        public decimal OpenPriceEquivalent { get; set; }
        public List<RelatedOrderInfo> RelatedOrders { get; set; }
        public string LegalEntity { get; set; }  
        public OriginatorType OpenOriginator { get; set; }
        public string ExternalProviderId { get; set; }
        public decimal SwapCommissionRate { get; set; }
        public decimal OpenCommissionRate { get; set; }
        public decimal CloseCommissionRate { get; set; }
        public decimal CommissionLot { get; set; }
        public string CloseMatchingEngineId { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal CloseFxPrice { get; set; }
        public decimal ClosePriceEquivalent { get; set; }
        public DateTime? StartClosingDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public OriginatorType? CloseOriginator { get; set; }
        public OrderCloseReason CloseReason { get; set; }
        public string CloseComment { get; set; }
        public List<string> CloseTrades { get; set; }
        public string FxAssetPairId { get; set; }
        public FxToAssetPairDirection FxToAssetPairDirection { get; set; }
        public DateTime? LastModified { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal ChargedPnl { get; set; }
        public string AdditionalInfo { get; set; }
        public PositionHistoryType HistoryType { get; set; }
        public DateTime HistoryTimestamp { get; set; }
        public bool ForceOpen { get; set; }
    }
}