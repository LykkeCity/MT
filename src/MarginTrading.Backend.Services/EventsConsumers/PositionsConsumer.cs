using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
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

                if (!string.IsNullOrEmpty(order.ParentPositionId))
                {
                    CloseExistingPosition(order);

                    return;
                }

                MatchOrderOnExistingPositions(order);
            }
        }

        private void CloseExistingPosition(Order order)
        {
            var position = _ordersCache.Positions.GetPositionById(order.ParentPositionId);

            position.Close(order.Executed.Value, order.MatchingEngineId, order.ExecutionPrice.Value,
                order.EquivalentRate, order.FxRate, order.Originator, order.OrderType.GetCloseReason(), order.Comment,
                order.Id);

            _ordersCache.Positions.Remove(position);

            SendPositionHistoryEvent(position, PositionHistoryTypeContract.Close,
                position.ChargedPnL, order, Math.Abs(position.Volume));

            CancelRelatedOrders(position.RelatedOrders, order.CorrelationId);
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
                order.Executed.Value, order.Id, order.ExecutionPrice.Value, order.FxRate, order.EquivalentAsset,
                order.EquivalentRate, order.RelatedOrders, order.LegalEntity, order.Originator,
                order.ExternalProviderId);
            
            var defaultMatchingEngine = _meRouter.GetMatchingEngineForClose(position);

            var closePrice = defaultMatchingEngine.GetPriceForClose(position.AssetPairId, position.Volume,
                position.ExternalProviderId);

            position.UpdateClosePrice(closePrice ?? order.ExecutionPrice.Value);

            _ordersCache.Positions.Add(position);

            SendPositionHistoryEvent(position, PositionHistoryTypeContract.Open, 0);
            
            ActivateRelatedOrders(position.RelatedOrders);
            
        }

        private void MatchOrderOnExistingPositions(Order order)
        {
            var leftVolumeToMatch = Math.Abs(order.Volume);
            
            var openedPositions =
                _ordersCache.Positions.GetPositionsByInstrumentAndAccount(order.AssetPairId, order.AccountId)
                    .Where(p => p.Direction == order.Direction.GetClosePositionDirection());
            
            foreach (var openedPosition in openedPositions)
            {
                if (Math.Abs(openedPosition.Volume) <= leftVolumeToMatch)
                {
                    openedPosition.Close(order.Executed.Value, order.MatchingEngineId, order.ExecutionPrice.Value,
                        order.EquivalentRate, order.FxRate, order.Originator, order.OrderType.GetCloseReason(),
                        order.Comment, order.Id);
                    
                    _ordersCache.Positions.Remove(openedPosition);

                    SendPositionHistoryEvent(openedPosition, PositionHistoryTypeContract.Close, openedPosition.ChargedPnL, order, Math.Abs(openedPosition.Volume));
                    
                    CancelRelatedOrders(openedPosition.RelatedOrders, order.CorrelationId);
                
                    leftVolumeToMatch = leftVolumeToMatch - Math.Abs(openedPosition.Volume);
                }
                else
                {
                    var chargedPnl = leftVolumeToMatch / openedPosition.Volume * openedPosition.ChargedPnL;
                    
                    openedPosition.PartiallyClose(order.Executed.Value, leftVolumeToMatch, order.Id, chargedPnl);

                    SendPositionHistoryEvent(openedPosition, PositionHistoryTypeContract.PartiallyClose, chargedPnl, order, Math.Abs(leftVolumeToMatch));

                    ChangeRelatedOrderVolume(openedPosition.RelatedOrders, -openedPosition.Volume);
                
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

        private void SendPositionHistoryEvent(Position position, PositionHistoryTypeContract historyType, decimal chargedPnl, Order dealOrder = null, decimal? dealVolume = null)
        {
            DealContract deal = null;

            if (dealOrder != null && dealVolume != null)
            {
                var sign = position.Volume > 0 ? 1 : -1;

                var fpl = (dealOrder.ExecutionPrice.Value - position.OpenPrice) *
                          dealOrder.FxRate * dealVolume.Value * sign;
                
                deal = new DealContract
                {
                    DealId = _identityGenerator.GenerateAlphanumericId(),
                    PositionId = position.Id,
                    Volume = dealVolume.Value,
                    Created = dealOrder.Executed.Value,
                    OpenTradeId = position.OpenTradeId,
                    CloseTradeId = dealOrder.Id,
                    OpenPrice = position.OpenPrice,
                    OpenFxPrice = position.OpenFxPrice,
                    ClosePrice = dealOrder.ExecutionPrice.Value,
                    CloseFxPrice = dealOrder.FxRate,    
                    Fpl = fpl,
                    AdditionalInfo = dealOrder.AdditionalInfo,
                    Originator = dealOrder.Originator.ToType<OriginatorTypeContract>()
                };
                
                var account = _accountsCacheService.Get(position.AccountId);
                var balanceDelta = fpl - chargedPnl;
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
                Timestamp = _dateService.Now()
            };

            _rabbitMqNotifyService.PositionHistory(historyEvent);
        }
        
        private void ActivateRelatedOrders(List<RelatedOrderInfo> relatedOrderInfos)
        {
            foreach (var relatedOrderInfo in relatedOrderInfos)
            {
                if (_ordersCache.Inactive.TryPopById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrder.Activate(_dateService.Now(), true);
                    _ordersCache.Active.Add(relatedOrder);
                    _orderActivatedEventChannel.SendEvent(this, new OrderActivatedEventArgs(relatedOrder));
                }
            }
        }
        
        private void CancelRelatedOrders(List<RelatedOrderInfo> relatedOrderInfos, string correlationId)
        {
            foreach (var relatedOrderInfo in relatedOrderInfos)
            {
                if (_ordersCache.Active.TryPopById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrder.Cancel(_dateService.Now(), OriginatorType.System, null, correlationId);
                    _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(relatedOrder));
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
                    relatedOrder.ChangeVolume(newVolume, _dateService.Now(), OriginatorType.System);
                    _orderChangedEventChannel.SendEvent(this, new OrderChangedEventArgs(relatedOrder));
                }
            }
        }
        
        private object GetLockObject(Order order)
        {
            return LockObjects.GetOrAdd(order.AccountId, new object());
        }
    }
}