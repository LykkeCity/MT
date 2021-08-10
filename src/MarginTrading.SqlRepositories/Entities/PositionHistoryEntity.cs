// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Common;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.SqlRepositories.Entities
{
    [UsedImplicitly]
    public class PositionHistoryEntity : IPositionHistory
    {
        public string Id { get; set; }
        public string DealId { get; set; }
        public long Code { get; set; }
        public string AssetPairId { get; set; }
        PositionDirection IPositionHistory.Direction => Direction.ParseEnum<PositionDirection>();
        public string Direction { get; set; }
        public decimal Volume { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountAssetId { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public string OpenMatchingEngineId { get; set; }
        public DateTime OpenDate { get; set; }
        public string OpenTradeId { get; set; }
        public string OpenOrderType { get; set; }
        OrderType IPositionHistory.OpenOrderType => OpenOrderType.ParseEnum<OrderType>();
        public decimal OpenOrderVolume { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal OpenFxPrice { get; set; }
        public string EquivalentAsset { get; set; }
        public decimal OpenPriceEquivalent { get; set; }
        public string LegalEntity { get; set; }
        OriginatorType IPositionHistory.OpenOriginator => OpenOriginator.ParseEnum<OriginatorType>();
        public string OpenOriginator { get; set; }
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
        OriginatorType? IPositionHistory.CloseOriginator => string.IsNullOrEmpty(CloseOriginator)
            ? (OriginatorType?) null
            : CloseOriginator.ParseEnum<OriginatorType>();
        public string CloseOriginator { get; set; }
        OrderCloseReason IPositionHistory.CloseReason => CloseReason.ParseEnum<OrderCloseReason>();
        public string CloseReason { get; set; }
        public string CloseComment { get; set; }
        public DateTime? LastModified { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal ChargedPnl { get; set; }
        public string AdditionalInfo { get; set; }
        PositionHistoryType IPositionHistory.HistoryType => HistoryType.ParseEnum<PositionHistoryType>();
        public string HistoryType { get; set; }

        public DateTime HistoryTimestamp { get; set; }

        List<RelatedOrderInfo> IPositionHistory.RelatedOrders => string.IsNullOrEmpty(RelatedOrders)
            ? new List<RelatedOrderInfo>()
            : RelatedOrders.DeserializeJson<List<RelatedOrderInfo>>();
        
        List<string> IPositionHistory.CloseTrades => string.IsNullOrEmpty(CloseTrades)
            ? new List<string>()
            : CloseTrades.DeserializeJson<List<string>>();

        public string FxAssetPairId { get; set; }
        public string FxToAssetPairDirection { get; set; }
        FxToAssetPairDirection IPositionHistory.FxToAssetPairDirection => FxToAssetPairDirection.ParseEnum<FxToAssetPairDirection>();

        public string RelatedOrders { get; set; }
        public string CloseTrades { get; set; }

        public bool ForceOpen { get; set; }
    }
}