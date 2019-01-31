using System.Collections.Generic;
using System.Security.Cryptography;
using MarginTrading.Backend.Contracts.Activities;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    public class OrderStateConsumer : IEventConsumer<OrderUpdateBaseEventArgs>
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly OrdersCache _ordersCache;
        private readonly IDateService _dateService;
        private readonly IEventChannel<OrderCancelledEventArgs> _orderCancelledEventChannel;
        private readonly IEventChannel<OrderChangedEventArgs> _orderChangedEventChannel;

        public OrderStateConsumer(IRabbitMqNotifyService rabbitMqNotifyService,
            OrdersCache ordersCache,
            IDateService dateService,
            IEventChannel<OrderCancelledEventArgs> orderCancelledEventChannel,
            IEventChannel<OrderChangedEventArgs> orderChangedEventChannel)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _ordersCache = ordersCache;
            _dateService = dateService;
            _orderCancelledEventChannel = orderCancelledEventChannel;
            _orderChangedEventChannel = orderChangedEventChannel;
        }

        void IEventConsumer<OrderUpdateBaseEventArgs>.ConsumeEvent(object sender, OrderUpdateBaseEventArgs ea)
        {
            SendOrderHistory(ea);

            switch (ea.UpdateType)
            {
                case OrderUpdateType.Cancel:
                case OrderUpdateType.Reject:
                    CancelRelatedOrders(
                        ea.Order.RelatedOrders,
                        ea.Order.CorrelationId,
                        OrderCancellationReason.BaseOrderCancelled);
                    RemoveRelatedOrderFromParent(ea.Order);
                    break;
            }
        }

        int IEventConsumer.ConsumerRank => 100;

        private void SendOrderHistory(OrderUpdateBaseEventArgs ea)
        {
            _rabbitMqNotifyService.OrderHistory(
                ea.Order,
                ea.UpdateType,
                ea.ActivitiesMetadata);
        }
        
        private void CancelRelatedOrders(List<RelatedOrderInfo> relatedOrderInfos, string correlationId,
            OrderCancellationReason reason)
        {
            var metadata = new OrderCancelledMetadata
            {
                Reason = reason
            };
            
            foreach (var relatedOrderInfo in relatedOrderInfos)
            {
                if (_ordersCache.Inactive.TryPopById(relatedOrderInfo.Id, out var inactiveRelatedOrder))
                {
                    inactiveRelatedOrder.Cancel(_dateService.Now(), null, correlationId);
                    _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(inactiveRelatedOrder, metadata));
                } 
                else if (_ordersCache.Active.TryPopById(relatedOrderInfo.Id, out var activeRelatedOrder))
                {
                    activeRelatedOrder.Cancel(_dateService.Now(), null, correlationId);
                    _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(activeRelatedOrder, metadata));
                }
            }
        }

        private void RemoveRelatedOrderFromParent(Order order)
        {
            if (!string.IsNullOrEmpty(order.ParentOrderId)
                && _ordersCache.TryGetOrderById(order.ParentOrderId, out var parentOrder))
            {
                var metadata = new OrderChangedMetadata
                {
                    UpdatedProperty = OrderChangedProperty.RelatedOrderRemoved,
                    OldValue = order.Id
                };
                
                parentOrder.RemoveRelatedOrder(order.Id);
                _orderChangedEventChannel.SendEvent(this, new OrderChangedEventArgs(parentOrder, metadata));
            }
            
            if (!string.IsNullOrEmpty(order.ParentPositionId)
                && _ordersCache.Positions.TryGetPositionById(order.ParentPositionId, out var parentPosition))
            {
                parentPosition.RemoveRelatedOrder(order.Id);
            }
        }
    }
}