using System;
using System.Threading.Tasks;
using Lykke.Common;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services.Client;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    public class OrderStateConsumer : NotificationSenderBase, IEventConsumer<OrderUpdateBaseEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IClientNotifyService _clientNotifyService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly ICqrsSender _cqrsSender;

        public OrderStateConsumer(IThreadSwitcher threadSwitcher, IClientNotifyService clientNotifyService,
            IAccountsCacheService accountsCacheService, IAppNotifications appNotifications,
            IClientAccountService clientAccountService, IRabbitMqNotifyService rabbitMqNotifyService,
            IAssetsCache assetsCache, IAssetPairsCache assetPairsCache, ICqrsSender cqrsSender) : base(appNotifications,
            clientAccountService, assetsCache, assetPairsCache)
        {
            _threadSwitcher = threadSwitcher;
            _clientNotifyService = clientNotifyService;
            _accountsCacheService = accountsCacheService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _cqrsSender = cqrsSender;
        }

        void IEventConsumer<OrderUpdateBaseEventArgs>.ConsumeEvent(object sender, OrderUpdateBaseEventArgs ea)
        {
            SendOrderHistory(ea);
            switch (ea.UpdateType)
            {
                case OrderUpdateType.Closing:
                case OrderUpdateType.Reject:
                    break;
                case OrderUpdateType.Activate:
                case OrderUpdateType.Place:
                    OnPlacedOrActivated(ea);
                    break;
                case OrderUpdateType.Cancel:
                    OnCancelled(ea);
                    break;
                case OrderUpdateType.Close:
                    OnClosed(ea);
                    break;
                case OrderUpdateType.ChangeOrderLimits:
                    OnLimitsChanged(ea);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ea.UpdateType), ea.UpdateType, string.Empty);
            }
        }

        int IEventConsumer.ConsumerRank => 100;

        private void OnClosed(OrderUpdateBaseEventArgs ea)
        {
            var order = ea.Order;
            var totalFpl = order.GetTotalFpl();
            var account = _accountsCacheService.Get(order.AccountId);
            _cqrsSender.PublishEvent(new PositionClosedEvent(account.Id, account.ClientId, order.Id, totalFpl));

            // todo: This should not be in a separate thread. Instead such things should be moved to a separate services
            _threadSwitcher.SwitchThread(() =>
            {
                _clientNotifyService.NotifyOrderChanged(order);
                return SendOrderChangedNotification(account.ClientId, order);
            });
        }

        private void OnCancelled(OrderUpdateBaseEventArgs ea)
        {
            var order = ea.Order;
            _threadSwitcher.SwitchThread(async () =>
            {
                _clientNotifyService.NotifyOrderChanged(order);
                var account = _accountsCacheService.Get(order.AccountId);
                await SendOrderChangedNotification(account.ClientId, order);
            });
        }

        private void OnPlacedOrActivated(OrderUpdateBaseEventArgs ea)
        {
            var order = ea.Order;
            _threadSwitcher.SwitchThread(async () =>
            {
                _clientNotifyService.NotifyOrderChanged(order);
                var account = _accountsCacheService.Get(order.AccountId);
                await SendOrderChangedNotification(account.ClientId, order);
            });
        }

        private void OnLimitsChanged(OrderUpdateBaseEventArgs ea)
        {
            var order = ea.Order;
            _threadSwitcher.SwitchThread(() =>
            {
                _clientNotifyService.NotifyOrderChanged(order);
                return Task.CompletedTask;
            });
        }

        private void SendOrderHistory(OrderUpdateBaseEventArgs ea)
        {
            _rabbitMqNotifyService.OrderHistory(ea.Order, ea.UpdateType);
        }
    }
}