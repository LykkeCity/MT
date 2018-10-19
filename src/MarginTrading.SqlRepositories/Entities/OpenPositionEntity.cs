using System;
using System.Collections.Generic;
using Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.SqlRepositories.Entities
{
    public class OpenPositionEntity
    {
        public string Id { get; set; }
        public long Code { get; set; }
        public string AssetPairId { get; set; }
        public string Direction { get; set; }
        public decimal Volume { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountAssetId { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public string OpenMatchingEngineId { get; set; }
        public DateTime OpenDate { get; set; }
        public string OpenTradeId { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal OpenFxPrice { get; set; }
        public string EquivalentAsset { get; set; }
        public decimal OpenPriceEquivalent { get; set; }
        public string LegalEntity { get; set; }
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
        public string CloseOriginator { get; set; }
        public string CloseReason { get; set; }
        public string CloseComment { get; set; }
        public string CloseTrades { get; set; }
        public DateTime? LastModified { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal ChargedPnL { get; set; }
        public decimal Margin { get; set; }

        public string RelatedOrders { get; set; }
        
        public DateTime HistoryTimestamp { get; set; }
        
        public static OpenPositionEntity Create(Position position, DateTime now)
        {
            return new OpenPositionEntity
            {
                AccountAssetId = position.AccountAssetId,
                AccountId = position.AccountId,
                AssetPairId = position.AssetPairId,
                CloseComment = position.CloseComment,
                CloseCommissionRate = position.CloseCommissionRate,
                CloseDate = position.CloseDate,
                CloseFxPrice = position.CloseFxPrice,
                CloseMatchingEngineId = position.CloseMatchingEngineId,
                CloseOriginator = position.CloseOriginator?.ToString(),
                ClosePrice = position.ClosePrice,
                ClosePriceEquivalent = position.ClosePriceEquivalent,
                CloseReason = position.CloseReason.ToString(),
                CloseTrades = position.CloseTrades.ToJson(),
                Code = position.Code,
                CommissionLot = position.CommissionLot,
                Direction = position.Direction.ToString(),
                EquivalentAsset = position.EquivalentAsset,
                ExpectedOpenPrice = position.ExpectedOpenPrice,
                ExternalProviderId = position.ExternalProviderId,
                Id = position.Id,
                LastModified = position.LastModified,
                LegalEntity = position.LegalEntity,
                OpenCommissionRate = position.OpenCommissionRate,
                OpenDate = position.OpenDate,
                OpenFxPrice = position.OpenFxPrice,
                OpenMatchingEngineId = position.OpenMatchingEngineId,
                OpenOriginator = position.OpenOriginator.ToString(),
                OpenPrice = position.OpenPrice,
                OpenPriceEquivalent = position.OpenPriceEquivalent,
                OpenTradeId = position.OpenTradeId,
                RelatedOrders = position.RelatedOrders.ToJson(),
                StartClosingDate = position.CloseDate,
                SwapCommissionRate = position.SwapCommissionRate,
                TotalPnL = position.GetFpl(),
                ChargedPnL = position.ChargedPnL,
                Margin = position.GetMarginMaintenance(),
                TradingConditionId = position.TradingConditionId,
                Volume = position.Volume,
                HistoryTimestamp = now,
            };
        }
    }
}
