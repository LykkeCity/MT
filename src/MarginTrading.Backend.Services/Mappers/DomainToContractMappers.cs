﻿using System.Linq;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.TradeMonitoring;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Common.Extensions;
using MatchedOrderContract = MarginTrading.Backend.Contracts.Orders.MatchedOrderContract;
using OrderDirectionContract = MarginTrading.Backend.Contracts.Orders.OrderDirectionContract;
using OrderStatusContract = MarginTrading.Backend.Contracts.Orders.OrderStatusContract;

namespace MarginTrading.Backend.Services.Mappers
{
    public static class DomainToContractMappers
    {
        public static OrderContract ConvertToContract(this Order order)
        {
            return new OrderContract
            {
                Id = order.Id,
                AccountId = order.AccountId,
                AssetPairId = order.AssetPairId,
                CreatedTimestamp = order.Created,
                Direction = order.Direction.ToType<OrderDirectionContract>(),
                ExecutionPrice = order.ExecutionPrice,
                FxRate = order.FxRate,
                ExpectedOpenPrice = order.Price,
                ForceOpen = order.ForceOpen,
                ModifiedTimestamp = order.LastModified,
                Originator = order.Originator.ToType<OriginatorTypeContract>(),
                ParentOrderId = order.ParentOrderId,
                PositionId = order.ParentPositionId,
                RelatedOrders = order.RelatedOrders.Select(o => o.Id).ToList(),
                Status = order.Status.ToType<OrderStatusContract>(),
                Type = order.OrderType.ToType<OrderTypeContract>(),
                ValidityTime = order.Validity,
                Volume = order.Volume,
                //------
                AccountAssetId = order.AccountAssetId,
                EquivalentAsset = order.EquivalentAsset,
                ActivatedTimestamp = order.Activated,
                CanceledTimestamp = order.Canceled,
                Code = order.Code,
                Comment = order.Comment,
                EquivalentRate = order.EquivalentRate,
                ExecutedTimestamp = order.Executed,
                ExecutionStartedTimestamp = order.ExecutionStarted,
                ExternalOrderId = order.ExternalOrderId,
                ExternalProviderId = order.ExternalProviderId,
                LegalEntity = order.LegalEntity,
                MatchedOrders = order.MatchedOrders.Select(o => new MatchedOrderContract
                {
                    OrderId = o.OrderId,
                    Volume = o.Volume,
                    Price = o.Price,
                    MarketMakerId = o.MarketMakerId,
                    LimitOrderLeftToMatch = o.LimitOrderLeftToMatch,
                    MatchedDate = o.MatchedDate,
                    IsExternal = o.IsExternal
                }).ToList(),
                MatchingEngineId = order.MatchingEngineId,
                Rejected = order.Rejected,
                RejectReason = order.RejectReason.ToType<OrderRejectReasonContract>(),
                RejectReasonText = order.RejectReasonText,
                RelatedOrderInfos = order.RelatedOrders.Select(o =>
                    new RelatedOrderInfoContract {Id = o.Id, Type = o.Type.ToType<OrderTypeContract>()}).ToList(),
                TradingConditionId = order.TradingConditionId
            };
        }
    }
}