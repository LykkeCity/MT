using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Notifications;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    public class OrderStateConsumer : IEventConsumer<OrderUpdateBaseEventArgs>
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly ICqrsSender _cqrsSender;

        public OrderStateConsumer(IAccountsCacheService accountsCacheService, 
            IRabbitMqNotifyService rabbitMqNotifyService,
            ICqrsSender cqrsSender)
        {
            _accountsCacheService = accountsCacheService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _cqrsSender = cqrsSender;
        }

        void IEventConsumer<OrderUpdateBaseEventArgs>.ConsumeEvent(object sender, OrderUpdateBaseEventArgs ea)
        {
            SendOrderHistory(ea);

            //if (ea.UpdateType == OrderUpdateType.Close)
            //{
            //    OnClosed(ea);
            //}
        }

        int IEventConsumer.ConsumerRank => 100;

        private void OnClosed(OrderUpdateBaseEventArgs ea)
        {
            //var order = ea.Order;
            //var totalFpl = order.GetTotalFpl();
            //var account = _accountsCacheService.Get(order.AccountId);
            //_cqrsSender.PublishEvent(new PositionClosedEvent(account.Id, account.ClientId, order.Id, totalFpl));
        }

        private void SendOrderHistory(OrderUpdateBaseEventArgs ea)
        {
            _rabbitMqNotifyService.OrderHistory(ea.Order, ea.UpdateType);
        }
    }
}