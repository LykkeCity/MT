// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.Activities;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    public class PositionsConsumer:
        IEventConsumer<OrderExecutedEventArgs>
    {
        private readonly OrdersCache _ordersCache;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IConvertService _convertService;
        private readonly IDateService _dateService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ICqrsSender _cqrsSender;
        private readonly IEventChannel<OrderCancelledEventArgs> _orderCancelledEventChannel;
        private readonly IEventChannel<OrderChangedEventArgs> _orderChangedEventChannel;
        private readonly IEventChannel<OrderActivatedEventArgs> _orderActivatedEventChannel;
        private readonly IMatchingEngineRouter _meRouter;
        private readonly ILog _log;

        private static readonly ConcurrentDictionary<string, object> LockObjects =
            new ConcurrentDictionary<string, object>();
        
        public int ConsumerRank => 100;

        public PositionsConsumer(OrdersCache ordersCache,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IConvertService convertService,
            IDateService dateService,
            IAccountsCacheService accountsCacheService,
            IAccountUpdateService accountUpdateService,
            IIdentityGenerator identityGenerator,
            ICqrsSender cqrsSender,
            IEventChannel<OrderCancelledEventArgs> orderCancelledEventChannel,
            IEventChannel<OrderChangedEventArgs> orderChangedEventChannel,
            IEventChannel<OrderActivatedEventArgs> orderActivatedEventChannel,
            IMatchingEngineRouter meRouter,
            ILog log)
        {
            _ordersCache = ordersCache;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _convertService = convertService;
            _dateService = dateService;
            _accountsCacheService = accountsCacheService;
            _accountUpdateService = accountUpdateService;
            _identityGenerator = identityGenerator;
            _cqrsSender = cqrsSender;
            _orderCancelledEventChannel = orderCancelledEventChannel;
            _orderChangedEventChannel = orderChangedEventChannel;
            _orderActivatedEventChannel = orderActivatedEventChannel;
            _meRouter = meRouter;
            _log = log;
        }
        
        public void ConsumeEvent(object sender, OrderExecutedEventArgs ea)
        {
            var order = ea.Order;

            lock (GetLockObject(order))
            {
                if (order.ForceOpen)
                {
                    OpenNewPosition(order, order.Volume);

                    return;
                }

                if (order.PositionsToBeClosed.Any())
                {
                    foreach (var positionId in order.PositionsToBeClosed)
                    {
                        var position = _ordersCache.Positions.GetPositionById(positionId);
                    
                        CloseExistingPosition(order, position);
                    }
                    
                    return;
                }

                MatchOrderOnExistingPositions(order);
            }
        }

        private void CloseExistingPosition(Order order, Position position)
        {
            position.Close(order.Executed.Value, order.MatchingEngineId, order.ExecutionPrice.Value,
                order.EquivalentRate, order.FxRate, order.Originator, order.OrderType.GetCloseReason(), order.Comment,
                order.Id);

            _ordersCache.Positions.Remove(position);

            SendPositionHistoryEvent(position, PositionHistoryTypeContract.Close,
                position.ChargedPnL, order.AdditionalInfo, order, Math.Abs(position.Volume));

            var reason = OrderCancellationReason.None;

            if (order.IsBasicOrder())
            {
                reason = OrderCancellationReason.ParentPositionClosed;
                CancelRelatedOrdersForOrder(order, order.CorrelationId, reason);
            }
            else
            {
                reason = OrderCancellationReason.ConnectedOrderExecuted;
            }
            
            CancelRelatedOrdersForPosition(position, order.CorrelationId, reason);
           
        }

        private void OpenNewPosition(Order order, decimal volume)
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

            var metadata = new PositionOpenMetadata {ExistingPositionIncreased = isPositionAlreadyExist};

            SendPositionHistoryEvent(position, PositionHistoryTypeContract.Open, 0, 
                order.AdditionalInfo, metadata: metadata);
            
            ActivateRelatedOrders(position);
            
        }

        private void MatchOrderOnExistingPositions(Order order)
        {
            var leftVolumeToMatch = Math.Abs(order.Volume);

            var openedPositions =
                _ordersCache.Positions.GetPositionsByInstrumentAndAccount(order.AssetPairId, order.AccountId)
                    .Where(p => p.Status != PositionStatus.Closing &&
                                p.Direction == order.Direction.GetClosePositionDirection());
            
            foreach (var openedPosition in openedPositions)
            {
                if (!openedPosition.TryStartClosing(_dateService.Now(), order.OrderType.GetCloseReason(), order
                    .Originator, ""))
                {
                    continue;
                }

                var absVolume = Math.Abs(openedPosition.Volume);

                if (absVolume <= leftVolumeToMatch)
                {
                    CloseExistingPosition(order, openedPosition);
                
                    leftVolumeToMatch = leftVolumeToMatch - absVolume;
                }
                else
                {
                    var chargedPnl = leftVolumeToMatch / absVolume * openedPosition.ChargedPnL;
                    
                    openedPosition.PartiallyClose(order.Executed.Value, leftVolumeToMatch, order.Id, chargedPnl);

                    SendPositionHistoryEvent(openedPosition, PositionHistoryTypeContract.PartiallyClose, chargedPnl, 
                        order.AdditionalInfo, order, Math.Abs(leftVolumeToMatch));

                    ChangeRelatedOrderVolume(openedPosition.RelatedOrders, -openedPosition.Volume);
                    
                    CancelRelatedOrdersForOrder(order, order.CorrelationId, OrderCancellationReason.ParentPositionClosed);
                
                    openedPosition.CancelClosing(_dateService.Now());
                    
                    leftVolumeToMatch = 0;
                }

                if (leftVolumeToMatch <= 0)
                    break;
            }

            if (leftVolumeToMatch > 0)
            {
                var volume = order.Volume > 0 ? leftVolumeToMatch : -leftVolumeToMatch;
                
                OpenNewPosition(order, volume);
            }
        }

        private void SendPositionHistoryEvent(Position position, PositionHistoryTypeContract historyType, 
            decimal chargedPnl, string orderAdditionalInfo, Order dealOrder = null, decimal? dealVolume = null, 
            PositionOpenMetadata metadata = null)
        {
            DealContract deal = null;

            if (dealOrder != null && dealVolume != null)
            {
                var sign = position.Volume > 0 ? 1 : -1;

                var accountBaseAssetAccuracy = AssetsConstants.DefaultAssetAccuracy;

                var fpl = Math.Round((dealOrder.ExecutionPrice.Value - position.OpenPrice) *
                                     dealOrder.FxRate * dealVolume.Value * sign, accountBaseAssetAccuracy);
                var balanceDelta = fpl - Math.Round(chargedPnl, accountBaseAssetAccuracy);

                var dealId = historyType == PositionHistoryTypeContract.Close
                    ? position.Id
                    : _identityGenerator.GenerateAlphanumericId();
                
                deal = new DealContract
                {
                    DealId = dealId,
                    PositionId = position.Id,
                    Volume = dealVolume.Value,
                    Created = dealOrder.Executed.Value,
                    OpenTradeId = position.OpenTradeId,
                    OpenOrderType = position.OpenOrderType.ToType<OrderTypeContract>(),
                    OpenOrderVolume = position.OpenOrderVolume,
                    OpenOrderExpectedPrice = position.ExpectedOpenPrice,
                    CloseTradeId = dealOrder.Id,
                    CloseOrderType = dealOrder.OrderType.ToType<OrderTypeContract>(),
                    CloseOrderVolume = dealOrder.Volume,
                    CloseOrderExpectedPrice = dealOrder.Price,
                    OpenPrice = position.OpenPrice,
                    OpenFxPrice = position.OpenFxPrice,
                    ClosePrice = dealOrder.ExecutionPrice.Value,
                    CloseFxPrice = dealOrder.FxRate,    
                    Fpl = fpl,
                    PnlOfTheLastDay = balanceDelta,
                    AdditionalInfo = dealOrder.AdditionalInfo,
                    Originator = dealOrder.Originator.ToType<OriginatorTypeContract>()
                };
                
                var account = _accountsCacheService.Get(position.AccountId);
                
                _cqrsSender.PublishEvent(new PositionClosedEvent(account.Id, account.ClientId,
                    deal.DealId, position.AssetPairId, balanceDelta));
            
                _accountUpdateService.FreezeUnconfirmedMargin(position.AccountId, deal.DealId, balanceDelta)
                    .GetAwaiter().GetResult();//todo consider making this async or pass to broker
            }

            var positionContract = _convertService.Convert<Position, PositionContract>(position,
                o => o.ConfigureMap(MemberList.Destination).ForMember(x => x.TotalPnL, c => c.Ignore()));
            positionContract.TotalPnL = position.GetFpl();

            var historyEvent = new PositionHistoryEvent
            {
                PositionSnapshot = positionContract,
                Deal = deal,
                EventType = historyType,
                Timestamp = _dateService.Now(),
                ActivitiesMetadata = metadata?.ToJson(),
                OrderAdditionalInfo = orderAdditionalInfo,
            };

            _rabbitMqNotifyService.PositionHistory(historyEvent);
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
        
        private void CancelRelatedOrdersForPosition(Position position, string correlationId,
            OrderCancellationReason reason)
        {
            var metadata = new OrderCancelledMetadata {Reason = reason.ToType<OrderCancellationReasonContract>()};
            
            foreach (var relatedOrderInfo in position.RelatedOrders)
            {
                if (_ordersCache.Active.TryPopById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrder.Cancel(_dateService.Now(), null, correlationId);
                    _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(relatedOrder, metadata));
                }
            }
        }
        
        private void CancelRelatedOrdersForOrder(Order order, string correlationId,
            OrderCancellationReason reason)
        {
            var metadata = new OrderCancelledMetadata {Reason = reason.ToType<OrderCancellationReasonContract>()};
            
            foreach (var relatedOrderInfo in order.RelatedOrders)
            {
                if (_ordersCache.Inactive.TryPopById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrder.Cancel(_dateService.Now(), null, correlationId);
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