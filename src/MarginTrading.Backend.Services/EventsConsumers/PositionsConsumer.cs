// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.Activities;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Builders;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    public class PositionsConsumer:
        IEventConsumer<OrderExecutedEventArgs>
    {
        private readonly OrdersCache _ordersCache;
        private readonly IDateService _dateService;
        private readonly IEventChannel<OrderCancelledEventArgs> _orderCancelledEventChannel;
        private readonly IEventChannel<OrderChangedEventArgs> _orderChangedEventChannel;
        private readonly IEventChannel<OrderActivatedEventArgs> _orderActivatedEventChannel;
        private readonly IMatchingEngineRouter _meRouter;
        private readonly ILog _log;
        private readonly IPositionHistoryHandler _positionHistoryHandler;
        private readonly IAccountUpdateService _accountUpdateService;

        private static readonly ConcurrentDictionary<string, object> LockObjects =
            new ConcurrentDictionary<string, object>();
        
        public int ConsumerRank => 100;

        public PositionsConsumer(OrdersCache ordersCache,
            IDateService dateService,
            IEventChannel<OrderCancelledEventArgs> orderCancelledEventChannel,
            IEventChannel<OrderChangedEventArgs> orderChangedEventChannel,
            IEventChannel<OrderActivatedEventArgs> orderActivatedEventChannel,
            IMatchingEngineRouter meRouter,
            ILog log,
            IPositionHistoryHandler positionHistoryHandler,
            IAccountUpdateService accountUpdateService)
        {
            _ordersCache = ordersCache;
            _dateService = dateService;
            _orderCancelledEventChannel = orderCancelledEventChannel;
            _orderChangedEventChannel = orderChangedEventChannel;
            _orderActivatedEventChannel = orderActivatedEventChannel;
            _meRouter = meRouter;
            _log = log;
            _positionHistoryHandler = positionHistoryHandler;
            _accountUpdateService = accountUpdateService;
        }
        
        // todo: move business logic out of consumer
        public void ConsumeEvent(object sender, OrderExecutedEventArgs ea)
        {
            var order = ea.Order;

            lock (GetLockObject(order))
            {
                if (order.ForceOpen)
                {
                    OpenNewPosition(order, order.Volume).GetAwaiter().GetResult();

                    return;
                }

                if (order.PositionsToBeClosed.Any())
                {
                    foreach (var positionId in order.PositionsToBeClosed)
                    {
                        var position = _ordersCache.Positions.GetPositionById(positionId);
                    
                        CloseExistingPosition(order, position).GetAwaiter().GetResult();
                    }
                    
                    return;
                }

                MatchOrderOnExistingPositions(order).GetAwaiter().GetResult();
            }
        }

        private async Task CloseExistingPosition(Order order, Position position)
        {
            position.Close(order.Executed.Value, order.MatchingEngineId, order.ExecutionPrice.Value,
                order.EquivalentRate, order.FxRate, order.Originator, order.OrderType.GetCloseReason(), order.Comment,
                order.Id);

            _ordersCache.Positions.Remove(position);

            var deal = DealDirector.Construct(new DealBuilder(position, order));
            await _accountUpdateService.FreezeUnconfirmedMargin(position.AccountId, deal.DealId, deal.PnlOfTheLastDay);
            await _positionHistoryHandler.HandleClosePosition(position, deal, order.AdditionalInfo);

            OrderCancellationReason reason;

            if (order.IsBasicOrder())
            {
                reason = OrderCancellationReason.ParentPositionClosed;
                CancelRelatedOrdersForOrder(order, reason);
            }
            else
            {
                reason = OrderCancellationReason.ConnectedOrderExecuted;
            }
            
            CancelRelatedOrdersForPosition(position, reason);
        }

        private async Task OpenNewPosition(Order order, decimal volume)
        {
            if (order.ExecutionPrice == null)
            {
                _log.WriteWarning(nameof(OpenNewPosition), order.ToJson(),
                    "Execution price is null. Position was not opened");
                return;
            }

            var position = new Position(order.Id, order.Code, order.AssetPairId, volume, order.AccountId,
                order.TradingConditionId, order.AccountAssetId, order.Price, order.MatchingEngineId,
                order.Executed.Value, order.Id, order.OrderType, order.Volume, order.ExecutionPrice.Value, order.FxRate,
                order.EquivalentAsset, order.EquivalentRate, order.RelatedOrders, order.LegalEntity, order.Originator,
                order.ExternalProviderId, order.FxAssetPairId, order.FxToAssetPairDirection, order.AdditionalInfo, order.ForceOpen);
            
            var defaultMatchingEngine = _meRouter.GetMatchingEngineForClose(position.OpenMatchingEngineId);

            var closePrice = defaultMatchingEngine.GetPriceForClose(position.AssetPairId, position.Volume,
                position.ExternalProviderId);

            position.UpdateClosePrice(closePrice ?? order.ExecutionPrice.Value);

            var isPositionAlreadyExist = _ordersCache.Positions.GetPositionsByInstrumentAndAccount(
                position.AssetPairId,
                position.AccountId).Any(p => p.Direction == position.Direction);

            _ordersCache.Positions.Add(position);

            await _positionHistoryHandler.HandleOpenPosition(position, order.AdditionalInfo,
                new PositionOpenMetadata { ExistingPositionIncreased = isPositionAlreadyExist });

            ActivateRelatedOrders(position);
            
        }

        private async Task MatchOrderOnExistingPositions(Order order)
        {
            var leftVolumeToMatch = Math.Abs(order.Volume);

            var openedPositions =
                _ordersCache.Positions.GetPositionsByInstrumentAndAccount(order.AssetPairId, order.AccountId)
                    .Where(p => p.Status == PositionStatus.Active &&
                                p.Direction == order.Direction.GetClosePositionDirection());
            
            foreach (var openedPosition in openedPositions)
            {
                var (closingStarted, reasonIfNot) = openedPosition.TryStartClosing(_dateService.Now(),
                    order.OrderType.GetCloseReason(),
                    order.Originator,
                    string.Empty);

                if (!closingStarted)
                {
                    _log.WriteWarning(nameof(MatchOrderOnExistingPositions), order.ToJson(),
                        $"Couldn't start position closing due to: {reasonIfNot}");
                    continue;
                }

                var absVolume = Math.Abs(openedPosition.Volume);

                if (absVolume <= leftVolumeToMatch)
                {
                    await CloseExistingPosition(order, openedPosition);
                
                    leftVolumeToMatch = leftVolumeToMatch - absVolume;
                }
                else
                {
                    var chargedPnl = leftVolumeToMatch / absVolume * openedPosition.ChargedPnL;
                    
                    openedPosition.PartiallyClose(order.Executed.Value, leftVolumeToMatch, order.Id, chargedPnl);

                    var deal = DealDirector.Construct(new PartialDealBuilder(openedPosition, order, leftVolumeToMatch));
                    await _accountUpdateService.FreezeUnconfirmedMargin(openedPosition.AccountId, deal.DealId, deal.PnlOfTheLastDay);
                    await _positionHistoryHandler.HandlePartialClosePosition(openedPosition, deal, order.AdditionalInfo);

                    ChangeRelatedOrderVolume(openedPosition.RelatedOrders, -openedPosition.Volume);
                    
                    CancelRelatedOrdersForOrder(order, OrderCancellationReason.ParentPositionClosed);
                
                    openedPosition.CancelClosing(_dateService.Now());
                    
                    leftVolumeToMatch = 0;
                }

                if (leftVolumeToMatch <= 0)
                    break;
            }

            if (leftVolumeToMatch > 0)
            {
                var volume = order.Volume > 0 ? leftVolumeToMatch : -leftVolumeToMatch;
                
                await OpenNewPosition(order, volume);
            }
        }

        private void ActivateRelatedOrders(Position position)
        {
            foreach (var relatedOrderInfo in position.RelatedOrders)
            {
                if (_ordersCache.Inactive.TryPopById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrder.Activate(_dateService.Now(), true, position.ClosePrice);
                    _ordersCache.Active.Add(relatedOrder);
                    _orderActivatedEventChannel.SendEvent(this, new OrderActivatedEventArgs(relatedOrder));
                }
            }
        }
        
        private void CancelRelatedOrdersForPosition(Position position, OrderCancellationReason reason)
        {
            var metadata = new OrderCancelledMetadata {Reason = reason.ToType<OrderCancellationReasonContract>()};
            
            foreach (var relatedOrderInfo in position.RelatedOrders)
            {
                if (_ordersCache.Active.TryPopById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrder.Cancel(_dateService.Now(), null);
                    _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(relatedOrder, metadata));
                }
            }
        }
        
        private void CancelRelatedOrdersForOrder(Order order, OrderCancellationReason reason)
        {
            var metadata = new OrderCancelledMetadata {Reason = reason.ToType<OrderCancellationReasonContract>()};
            
            foreach (var relatedOrderInfo in order.RelatedOrders)
            {
                if (_ordersCache.Inactive.TryPopById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrder.Cancel(_dateService.Now(), null);
                    _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(relatedOrder, metadata));
                }
            }
        }
        
        private void ChangeRelatedOrderVolume(List<RelatedOrderInfo> relatedOrderInfos, decimal newVolume)
        {
            foreach (var relatedOrderInfo in relatedOrderInfos)
            {
                if (_ordersCache.TryGetOrderById(relatedOrderInfo.Id, out var relatedOrder)
                    && relatedOrder.Volume != newVolume)
                {
                    var oldVolume = relatedOrder.Volume;
                    
                    relatedOrder.ChangeVolume(newVolume, _dateService.Now(), OriginatorType.System);
                    var metadata = new OrderChangedMetadata
                    {
                        UpdatedProperty = OrderChangedProperty.Volume,
                        OldValue = oldVolume.ToString("F2")
                    };

                    _orderChangedEventChannel.SendEvent(this, new OrderChangedEventArgs(relatedOrder, metadata));
                }
            }
        }
        
        private object GetLockObject(Order order)
        {
            return LockObjects.GetOrAdd(order.AccountId, new object());
        }
    }
}