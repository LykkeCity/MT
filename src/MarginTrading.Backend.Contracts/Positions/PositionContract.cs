// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.TradeMonitoring;

namespace MarginTrading.Backend.Contracts.Positions
{
    [UsedImplicitly]
    public class PositionContract
    {
        public string Id { get; set; }
        public long Code { get; set; }
        public string AssetPairId { get; set; }
        public PositionDirectionContract Direction { get; set; }
        public decimal Volume { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountAssetId { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public string OpenMatchingEngineId { get; set; }
        public DateTime OpenDate { get; set; }
        public string OpenTradeId { get; set; }
        public OrderTypeContract OpenOrderType { get; set; }
        public decimal OpenOrderVolume { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal OpenFxPrice { get; set; }
        public string EquivalentAsset { get; set; }
        public decimal OpenPriceEquivalent { get; set; }
        public List<RelatedOrderInfoContract> RelatedOrders { get; set; }
        public string LegalEntity { get; set; }  
        public OriginatorTypeContract OpenOriginator { get; set; }
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
        public OriginatorTypeContract? CloseOriginator { get; set; }
        public PositionCloseReasonContract CloseReason { get; set; }
        public string CloseComment { get; set; }
        public List<string> CloseTrades { get; set; }
        public string FxAssetPairId { get; set; }
        public FxToAssetPairDirectionContract FxToAssetPairDirection { get; set; }
        public DateTime? LastModified { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal ChargedPnl { get; set; }
        public string AdditionalInfo { get; set; }
        public bool ForceOpen { get; set; }
    }
}