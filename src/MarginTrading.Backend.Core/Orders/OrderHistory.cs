using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Core.Orders
{
    public class OrderHistory : IOrderHistory
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public string AssetPairId { get; set; }
        public string ParentOrderId { get; set; }
        public string PositionId { get; set; }
        public OrderDirection Direction { get; set; }
        public OrderType Type { get; set; }
        public OrderStatus Status { get; set; }
        public OrderFillType FillType { get; set; }
        public OriginatorType Originator { get; set; }
        public OriginatorType? CancellationOriginator { get; set; }
        public decimal Volume { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal? ExecutionPrice { get; set; }
        public decimal FxRate { get; set; }
        public string FxAssetPairId { get; set; }
        public FxToAssetPairDirection FxToAssetPairDirection { get; set; }
        public bool ForceOpen { get; set; }
        public DateTime? ValidityTime { get; set; }
        public DateTime CreatedTimestamp { get; set; }
        public DateTime ModifiedTimestamp { get; set; }
        public long Code { get; set; }
        public DateTime? ActivatedTimestamp { get; set; }
        public DateTime? ExecutionStartedTimestamp { get; set; }
        public DateTime? ExecutedTimestamp { get; set; }
        public DateTime? CanceledTimestamp { get; set; }
        public DateTime? Rejected { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountAssetId { get; set; }
        public string EquivalentAsset { get; set; }
        public decimal EquivalentRate { get; set; }
        public OrderRejectReason RejectReason { get; set; }
        public string RejectReasonText { get; set; }
        public string Comment { get; set; }
        public string ExternalOrderId { get; set; }
        public string ExternalProviderId { get; set; }
        public string MatchingEngineId { get; set; }
        public string LegalEntity { get; set; }
        public List<MatchedOrder> MatchedOrders { get; set; }
        public List<RelatedOrderInfo> RelatedOrderInfos { get; set; }
        public OrderUpdateType UpdateType { get; set; }
        public string AdditionalInfo { get; set; }
        public string CorrelationId { get; set; }
        public int PendingOrderRetriesCount { get; set; }

        public static OrderHistory Create(IOrderHistory orderHistory)
        {
            return new OrderHistory
            {
                Id = orderHistory.Id,
                AccountId = orderHistory.AccountId,
                AssetPairId = orderHistory.AssetPairId,
                ParentOrderId = orderHistory.ParentOrderId,
                PositionId = orderHistory.PositionId,
                Direction = orderHistory.Direction,
                Type = orderHistory.Type,
                Status = orderHistory.Status,
                FillType = orderHistory.FillType,
                Originator = orderHistory.Originator,
                CancellationOriginator = orderHistory.CancellationOriginator,
                Volume =orderHistory.Volume,
                ExpectedOpenPrice = orderHistory.ExpectedOpenPrice,
                ExecutionPrice = orderHistory.ExecutionPrice,
                FxRate = orderHistory.FxRate,
                FxAssetPairId = orderHistory.FxAssetPairId,
                FxToAssetPairDirection = orderHistory.FxToAssetPairDirection,
                ForceOpen = orderHistory.ForceOpen,
                ValidityTime = orderHistory.ValidityTime,
                CreatedTimestamp = orderHistory.CreatedTimestamp,
                ModifiedTimestamp = orderHistory.ModifiedTimestamp,
                Code = orderHistory.Code,
                ActivatedTimestamp = orderHistory.ActivatedTimestamp,
                ExecutionStartedTimestamp = orderHistory.ExecutionStartedTimestamp,
                ExecutedTimestamp = orderHistory.ExecutedTimestamp,
                CanceledTimestamp = orderHistory.CanceledTimestamp,
                Rejected = orderHistory.Rejected,
                TradingConditionId = orderHistory.TradingConditionId,
                AccountAssetId = orderHistory.AccountAssetId,
                EquivalentAsset = orderHistory.EquivalentAsset,
                EquivalentRate = orderHistory.EquivalentRate,
                RejectReason = orderHistory.RejectReason,
                RejectReasonText = orderHistory.RejectReasonText,
                Comment = orderHistory.Comment,
                ExternalOrderId = orderHistory.ExternalOrderId,
                ExternalProviderId = orderHistory.ExternalProviderId,
                MatchingEngineId = orderHistory.MatchingEngineId,
                LegalEntity = orderHistory.LegalEntity,
                MatchedOrders = orderHistory.MatchedOrders,
                RelatedOrderInfos = orderHistory.RelatedOrderInfos,
                UpdateType = orderHistory.UpdateType,
                AdditionalInfo = orderHistory.AdditionalInfo,
                CorrelationId = orderHistory.CorrelationId,
                PendingOrderRetriesCount = orderHistory.PendingOrderRetriesCount,
            };
        }
    }
}