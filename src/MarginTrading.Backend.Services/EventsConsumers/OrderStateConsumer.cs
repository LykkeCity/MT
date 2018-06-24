using System.Collections.Generic;
using MarginTrading.Backend.Core.Orders;
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

        public OrderStateConsumer(IRabbitMqNotifyService rabbitMqNotifyService,
            OrdersCache ordersCache,
            IDateService dateService,
            IEventChannel<OrderCancelledEventArgs> orderCancelledEventChannel)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _ordersCache = ordersCache;
            _dateService = dateService;
            _orderCancelledEventChannel = orderCancelledEventChannel;
        }

        void IEventConsumer<OrderUpdateBaseEventArgs>.ConsumeEvent(object sender, OrderUpdateBaseEventArgs ea)
        {
            SendOrderHistory(ea);

            switch (ea.UpdateType)
            {
                case OrderUpdateType.Cancel:
                case OrderUpdateType.Reject:
                    CancelRelatedOrders(ea.Order.RelatedOrders);
                    break;
            }
        }

        int IEventConsumer.ConsumerRank => 100;

        private void SendOrderHistory(OrderUpdateBaseEventArgs ea)
        {
            _rabbitMqNotifyService.OrderHistory(ea.Order, ea.UpdateType);
        }
        
        private void CancelRelatedOrders(List<RelatedOrderInfo> relatedOrderInfos)
        {
            foreach (var relatedOrderInfo in relatedOrderInfos)
            {
                if (_ordersCache.Inactive.TryPopById(relatedOrderInfo.Id, out var inactiveRelatedOrder))
                {
                    inactiveRelatedOrder.Cancel(_dateService.Now(), null);
                    _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(inactiveRelatedOrder));
                } 
                else if (_ordersCache.Active.TryPopById(relatedOrderInfo.Id, out var activeRelatedOrder))
                {
                    activeRelatedOrder.Cancel(_dateService.Now(), null);
                    _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(activeRelatedOrder));
                }
            }
        }
    }
}