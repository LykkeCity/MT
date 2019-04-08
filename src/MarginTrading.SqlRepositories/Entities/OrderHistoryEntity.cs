using System;
using System.Collections.Generic;
using Common;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.SqlRepositories.Entities
{
    [UsedImplicitly]
    public class OrderHistoryEntity : IOrderHistory
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public string AssetPairId { get; set; }
        public string ParentOrderId { get; set; }
        public string PositionId { get; set; }
        OrderDirection IOrderHistory.Direction => Direction.ParseEnum<OrderDirection>();
        public string Direction { get; set; }
        OrderType IOrderHistory.Type => Type.ParseEnum<OrderType>();
        public string Type { get; set; }
        OrderStatus IOrderHistory.Status => Status.ParseEnum<OrderStatus>();
        public string FillType { get; set; }
        OrderFillType IOrderHistory.FillType => FillType.ParseEnum<OrderFillType>();
        public string Status { get; set; }
        OriginatorType IOrderHistory.Originator => Originator.ParseEnum<OriginatorType>();
        public string Originator { get; set; }
        OriginatorType? IOrderHistory.CancellationOriginator => Originator?.ParseEnum<OriginatorType>();
        public string CancellationOriginator { get; set; }
        public decimal Volume { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal? ExecutionPrice { get; set; }
        public decimal FxRate { get; set; }
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
        OrderRejectReason IOrderHistory.RejectReason => RejectReason.ParseEnum<OrderRejectReason>();
        public string RejectReason { get; set; }
        public string RejectReasonText { get; set; }
        public string Comment { get; set; }
        public string ExternalOrderId { get; set; }
        public string ExternalProviderId { get; set; }
        public string MatchingEngineId { get; set; }
        public string LegalEntity { get; set; }

        List<MatchedOrder> IOrderHistory.MatchedOrders => string.IsNullOrEmpty(MatchedOrders)
            ? new List<MatchedOrder>()
            : MatchedOrders.DeserializeJson<List<MatchedOrder>>();

        List<RelatedOrderInfo> IOrderHistory.RelatedOrderInfos => string.IsNullOrEmpty(RelatedOrderInfos)
            ? new List<RelatedOrderInfo>()
            : RelatedOrderInfos.DeserializeJson<List<RelatedOrderInfo>>();

        OrderUpdateType IOrderHistory.UpdateType => UpdateType.ParseEnum<OrderUpdateType>();
        public string UpdateType { get; set; }
        public string AdditionalInfo { get; set; }
        public string CorrelationId { get; set; }
        public int PendingOrderRetriesCount { get; set; }
        public string MatchedOrders { get; set; }
        public string RelatedOrderInfos { get; set; }
    }
}