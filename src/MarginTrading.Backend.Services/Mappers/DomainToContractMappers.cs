// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Contracts.TradeMonitoring;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Common.Extensions;
using MatchedOrderContract = MarginTrading.Backend.Contracts.Orders.MatchedOrderContract;
using OrderDirectionContract = MarginTrading.Backend.Contracts.Orders.OrderDirectionContract;
using OrderStatusContract = MarginTrading.Backend.Contracts.Orders.OrderStatusContract;

namespace MarginTrading.Backend.Services.Mappers
{
    public static class DomainToContractMappers
    {
        public static OrderContract ConvertToContract(this Order order, IOrderReader orderReader)
        {
            var relatedOrders = new List<Order>();

            foreach (var relatedOrderInfo in order.RelatedOrders)
            {
                if (orderReader.TryGetOrderById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrders.Add(relatedOrder);
                }
            }

            return order.ConvertToContract(relatedOrders);
        }

        public static OrderContract ConvertToContract(this Order order, List<Order> relatedOrders)
        {
            RelatedOrderInfoContract Map(RelatedOrderInfo relatedOrderInfo)
            {
                var relateOrder = relatedOrders.FirstOrDefault(o => o.Id == relatedOrderInfo.Id);

                if (relateOrder == null)
                {
                    return null;
                }

                return new RelatedOrderInfoContract
                {
                    Id = relateOrder.Id,
                    Price = relateOrder.Price ?? 0,
                    Type = relateOrder.OrderType.ToType<OrderTypeContract>(),
                    Status = relateOrder.Status.ToType<OrderStatusContract>(),
                    ModifiedTimestamp = relateOrder.LastModified,
                    TrailingDistance = relateOrder.TrailingDistance
                };
            }

            return new OrderContract
            {
                Id = order.Id,
                AccountId = order.AccountId,
                AssetPairId = order.AssetPairId,
                CreatedTimestamp = order.Created,
                Direction = order.Direction.ToType<OrderDirectionContract>(),
                ExecutionPrice = order.ExecutionPrice,
                FxRate = order.FxRate,
                FxAssetPairId = order.FxAssetPairId,
                FxToAssetPairDirection = order.FxToAssetPairDirection.ToType<FxToAssetPairDirectionContract>(),
                ExpectedOpenPrice = order.Price,
                ForceOpen = order.ForceOpen,
                ModifiedTimestamp = order.LastModified,
                Originator = order.Originator.ToType<OriginatorTypeContract>(),
                ParentOrderId = order.ParentOrderId,
                PositionId = order.ParentPositionId,
                RelatedOrders = order.RelatedOrders.Select(o => o.Id).ToList(),
                Status = order.Status.ToType<OrderStatusContract>(),
                FillType = order.FillType.ToType<OrderFillTypeContract>(),
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
                RelatedOrderInfos = order.RelatedOrders.Select(Map).Where(o => o != null).ToList(),
                TradingConditionId = order.TradingConditionId,
                AdditionalInfo = order.AdditionalInfo,
                CorrelationId = order.CorrelationId,
                PendingOrderRetriesCount = order.PendingOrderRetriesCount,
                TrailingDistance = order.TrailingDistance,
                HasOnBehalf = order.HasOnBehalf,
            };
        }

        public static OpenPositionContract ConvertToContract(this Position position, IOrderReader orderReader)
        {
            var relatedOrders = new List<RelatedOrderInfoContract>();

            foreach (var relatedOrderInfo in position.RelatedOrders)
            {
                if (orderReader.TryGetOrderById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrders.Add(new RelatedOrderInfoContract
                    {
                        Id = relatedOrder.Id,
                        Price = relatedOrder.Price ?? 0,
                        Type = relatedOrder.OrderType.ToType<OrderTypeContract>(),
                        Status = relatedOrder.Status.ToType<OrderStatusContract>(),
                        ModifiedTimestamp = relatedOrder.LastModified,
                        TrailingDistance = relatedOrder.TrailingDistance
                    });
                }
            }

            return new OpenPositionContract
            {
                AccountId = position.AccountId,
                AssetPairId = position.AssetPairId,
                CurrentVolume = position.Volume,
                Direction = position.Direction.ToType<PositionDirectionContract>(),
                Id = position.Id,
                OpenPrice = position.OpenPrice,
                OpenFxPrice = position.OpenFxPrice,
                ClosePrice = position.ClosePrice,
                ExpectedOpenPrice = position.ExpectedOpenPrice,
                OpenTradeId = position.OpenTradeId,
                OpenOrderType = position.OpenOrderType.ToType<OrderTypeContract>(),
                OpenOrderVolume = position.OpenOrderVolume,
                PnL = position.GetFpl(),
                UnrealizedPnl = position.GetUnrealisedFpl(),
                ChargedPnl = position.ChargedPnL,
                Margin = position.GetMarginMaintenance(),
                FxRate = position.CloseFxPrice,
                FxAssetPairId = position.FxAssetPairId,
                FxToAssetPairDirection = position.FxToAssetPairDirection.ToType<FxToAssetPairDirectionContract>(),
                RelatedOrders = position.RelatedOrders.Select(o => o.Id).ToList(),
                RelatedOrderInfos = relatedOrders,
                OpenTimestamp = position.OpenDate,
                ModifiedTimestamp = position.LastModified,
                TradeId = position.Id,
                AdditionalInfo = position.AdditionalInfo,
                Status = position.Status.ToType<PositionStatusContract>(),
                ForceOpen = position.ForceOpen
            };
        }

        public static AccountStatContract ConvertToContract(this IMarginTradingAccount account)
        {
            return new AccountStatContract
            {
                AccountId = account.Id,
                BaseAssetId = account.BaseAssetId,
                Balance = account.Balance,
                LastBalanceChangeTime = account.LastBalanceChangeTime,
                MarginCallLevel = account.GetMarginCall1Level(),
                StopOutLevel = account.GetStopOutLevel(),
                TotalCapital = account.GetTotalCapital(),
                FreeMargin = account.GetFreeMargin(),
                MarginAvailable = account.GetMarginAvailable(),
                UsedMargin = account.GetUsedMargin(),
                CurrentlyUsedMargin = account.GetCurrentlyUsedMargin(),
                InitiallyUsedMargin = account.GetInitiallyUsedMargin(),
                MarginInit = account.GetMarginInit(),
                PnL = account.GetPnl(),
                UnrealizedDailyPnl = account.GetUnrealizedDailyPnl(),
                OpenPositionsCount = account.GetOpenPositionsCount(),
                ActiveOrdersCount = account.GetActiveOrdersCount(),
                MarginUsageLevel = account.GetMarginUsageLevel(),
                LegalEntity = account.LegalEntity,
                IsInLiquidation = account.IsInLiquidation(),
                MarginNotificationLevel = account.GetAccountLevel().ToString()
            };
        }

        public static BestPriceContract ConvertToContract(this InstrumentBidAskPair arg)
        {
            return new BestPriceContract
            {
                Ask = arg.Ask,
                Bid = arg.Bid,
                Id = arg.Instrument,
                Timestamp = arg.Date,
            };
        }
    }
}