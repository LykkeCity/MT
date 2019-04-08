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
                price: orderHistory.ExpectedOpenPrice,
                equivalentAsset: orderHistory.EquivalentAsset,
                fillType: OrderFillType.FillOrKill, //todo we've got only FOK now
                comment: orderHistory.Comment,
                legalEntity: orderHistory.LegalEntity,
                forceOpen: orderHistory.ForceOpen,
                orderType: orderHistory.Type,
                parentOrderId: orderHistory.ParentOrderId,
                parentPositionId: orderHistory.PositionId,
                originator: orderHistory.Originator,
                equivalentRate: orderHistory.EquivalentRate,
                fxRate: orderHistory.FxRate,
                fxAssetPairId: string.Empty, //todo will be recalculated outside
                fxToAssetPairDirection: FxToAssetPairDirection.Straight, //todo this too
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
        public static bool Merge(this Position position, IPositionHistory positionHistory)
        {
            return position.SetIfDiffer(new Dictionary<string, object>
            {
                //todo
            });
        }

        public static Position FromHistory(this IPositionHistory positionHistory)
        {
            return new Position(
                id: positionHistory.Id,
                code: positionHistory.Code,
                assetPairId: positionHistory.AssetPairId,
                volume: positionHistory.Volume,
                accountId: positionHistory.AccountId,
                tradingConditionId: positionHistory.TradingConditionId,
                accountAssetId: positionHistory.AccountAssetId,
                expectedOpenPrice: positionHistory.ExpectedOpenPrice,
                openMatchingEngineId: positionHistory.OpenMatchingEngineId,
                openDate: positionHistory.OpenDate,
                openTradeId: positionHistory.OpenTradeId,
                openOrderType: OrderType.Market, //todo ???
                openOrderVolume: 0, //todo ??
                openPrice: positionHistory.OpenPrice,
                openFxPrice: positionHistory.OpenFxPrice,
                equivalentAsset: positionHistory.EquivalentAsset,
                openPriceEquivalent: positionHistory.OpenPriceEquivalent,
                relatedOrders: positionHistory.RelatedOrders,
                legalEntity: positionHistory.LegalEntity,
                openOriginator: positionHistory.OpenOriginator,
                externalProviderId: positionHistory.ExternalProviderId,
                fxAssetPairId: string.Empty, // todo will be recalculated outside
                fxToAssetPairDirection: FxToAssetPairDirection.Straight, // todo this too
                additionalInfo: "" //todo ??
            );
        }
    }
}





























