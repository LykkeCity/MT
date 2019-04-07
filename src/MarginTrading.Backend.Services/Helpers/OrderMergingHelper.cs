using System.Collections.Generic;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Mappers;

namespace MarginTrading.Backend.Services.Helpers
{
    public static class OrderMergingHelper
    {
        /// <summary>
        /// Return true if there was difference, false if items were the same.
        /// </summary>
        public static bool Merge(this Order order, IOrderHistory orderHistory)
        {
            return order.SetIfDiffer(new Dictionary<string, object>
            {
                {nameof(Order.Volume), orderHistory.Volume},
                {nameof(Order.Status), orderHistory.Status},
                {nameof(Order.ParentOrderId), orderHistory.ParentOrderId},
                {nameof(Order.ParentPositionId), orderHistory.PositionId},
                {nameof(Order.ExecutionPrice), orderHistory.ExecutionPrice},
                {nameof(Order.FxRate), orderHistory.FxRate},
                {nameof(Order.Validity), orderHistory.ValidityTime},
                {nameof(Order.LastModified), orderHistory.ModifiedTimestamp},
                {nameof(Order.Activated), orderHistory.ActivatedTimestamp},
                {nameof(Order.ExecutionStarted), orderHistory.ExecutionStartedTimestamp},
                {nameof(Order.Executed), orderHistory.ExecutedTimestamp},
                {nameof(Order.Canceled), orderHistory.CanceledTimestamp},
                {nameof(Order.Rejected), orderHistory.Rejected},
                {nameof(Order.EquivalentRate), orderHistory.EquivalentRate},
                {nameof(Order.RejectReason), orderHistory.RejectReason},
                {nameof(Order.RejectReasonText), orderHistory.RejectReasonText},
                {nameof(Order.ExternalOrderId), orderHistory.ExternalOrderId},
                {nameof(Order.ExternalProviderId), orderHistory.ExternalProviderId},
                {nameof(Order.MatchingEngineId), orderHistory.MatchingEngineId},
                {nameof(Order.MatchedOrders), orderHistory.MatchedOrders},
                {nameof(Order.RelatedOrders), orderHistory.RelatedOrderInfos},
                {nameof(Order.AdditionalInfo), orderHistory.AdditionalInfo},
                {nameof(Order.PendingOrderRetriesCount), orderHistory.PendingOrderRetriesCount},
            });
        }

        public static Order FromHistory(this IOrderHistory orderHistory)
        {
            return new Order(
                id: orderHistory.Id,
                code: orderHistory.Code,
                assetPairId: orderHistory.AssetPairId,
                volume: orderHistory.Volume,
                created: orderHistory.CreatedTimestamp,
                lastModified: orderHistory.ModifiedTimestamp,
                validity: orderHistory.ValidityTime,
                accountId: orderHistory.AccountId,
                tradingConditionId: orderHistory.TradingConditionId,
                accountAssetId: orderHistory.AccountAssetId,
                price: orderHistory.ExecutionPrice,
                equivalentAsset: orderHistory.EquivalentAsset,
                fillType: OrderFillType.FillOrKill, //todo ??
                comment: orderHistory.Comment,
                legalEntity: orderHistory.LegalEntity,
                forceOpen: orderHistory.ForceOpen,
                orderType: orderHistory.Type,
                parentOrderId: orderHistory.ParentOrderId,
                parentPositionId: orderHistory.PositionId,
                originator: orderHistory.Originator,
                equivalentRate: orderHistory.EquivalentRate,
                fxRate: orderHistory.FxRate,
                fxAssetPairId: "SymmetricAssetPair", //todo ??
                fxToAssetPairDirection: 0, //todo ??
                status: orderHistory.Status,
                additionalInfo: orderHistory.AdditionalInfo,
                correlationId: orderHistory.CorrelationId,
                positionsToBeClosed: string.IsNullOrWhiteSpace(orderHistory.PositionId)
                    ? new List<string>()
                    : new List<string> {orderHistory.PositionId},
                externalProviderId: orderHistory.ExternalProviderId
            );
        }
        
        /// <summary>
        /// Return true if there was difference, false if items were the same.
        /// </summary>
        public static bool Merge(this Position position, IPositionHistory positionHistory, out Position result)
        {
            result = null;
            return false;
        }

        public static Position FromHistory(this IPositionHistory positionHistory)
        {
            return new Position(

            );
        }
    }
}