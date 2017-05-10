using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Services.Events;
using MarginTrading.Services.Generated.ClientAccountServiceApi;
using MarginTrading.Services.Notifications;

namespace MarginTrading.Services
{
    // TODO: Rename by role
    public class OrderStateConsumer : SendNotificationBase,
        IEventConsumer<OrderPlacedEventArgs>,
        IEventConsumer<OrderClosedEventArgs>,
        IEventConsumer<OrderCancelledEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IClientNotifyService _clientNotifyService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly AccountManager _accountManager;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly ITransactionService _transactionService;
        private readonly IOrderActionService _orderActionService;

        public OrderStateConsumer(IThreadSwitcher threadSwitcher,
            IClientSettingsRepository clientSettingsRepository,
            IClientNotifyService clientNotifyService,
            IAccountsCacheService accountsCacheService,
            IAppNotifications appNotifications,
            IClientAccountService clientAccountService,
            AccountManager accountManager,
            IRabbitMqNotifyService rabbitMqNotifyService,
            ITransactionService transactionService,
            IOrderActionService orderActionService) : base(clientSettingsRepository,
            appNotifications,
            clientAccountService)
        {
            _threadSwitcher = threadSwitcher;
            _clientNotifyService = clientNotifyService;
            _accountsCacheService = accountsCacheService;
            _accountManager = accountManager;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _transactionService = transactionService;
            _orderActionService = orderActionService;
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<OrderClosedEventArgs>.ConsumeEvent(object sender, OrderClosedEventArgs ea)
        {
            var order = ea.Order;
            _threadSwitcher.SwitchThread(async () =>
            {
                var totalFpl = order.GetTotalFpl(order.AssetAccuracy);

                await _accountManager.UpdateBalance(order.ClientId, order.AccountId, totalFpl);

                var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

                await _transactionService.CreateTransactionsForClosedOrderAsync(order,
                    _rabbitMqNotifyService.TransactionCreated);

                await _orderActionService.CreateOrderActionForClosedMarketOrder(order,
                    _rabbitMqNotifyService.OrderActionCreated);

                await _rabbitMqNotifyService.AccountHistory(account.Id, account.ClientId, totalFpl,
                    account.Balance, AccountHistoryType.OrderClosed, $"Balance changed on order close (id = {order.Id})");

                await _rabbitMqNotifyService.OrderHistory(order);

                _clientNotifyService.NotifyOrderChanged(order);
                _clientNotifyService.NotifyAccountChanged(account);

                await SendNotification(order.ClientId, order.GetPushMessage(), order);
            });
        }

        void IEventConsumer<OrderCancelledEventArgs>.ConsumeEvent(object sender, OrderCancelledEventArgs ea)
        {
            var order = ea.Order;
            _threadSwitcher.SwitchThread(async () =>
            {
                await _transactionService.CreateTransactionsForCancelledOrderAsync(order,
                    _rabbitMqNotifyService.TransactionCreated);

                await _orderActionService.CreateOrderActionForCancelledMarketOrder(order,
                    _rabbitMqNotifyService.OrderActionCreated);

                _clientNotifyService.NotifyOrderChanged(order);
                await _rabbitMqNotifyService.OrderHistory(order);
                await SendNotification(order.ClientId, order.GetPushMessage(), order);
            });
        }

        void IEventConsumer<OrderPlacedEventArgs>.ConsumeEvent(object sender, OrderPlacedEventArgs ea)
        {
            var order = ea.Order;

            _threadSwitcher.SwitchThread(async () =>
            {
                await _transactionService.CreateTransactionsForOpenOrderAsync(order,
                    _rabbitMqNotifyService.TransactionCreated);

                await _orderActionService.CreateOrderActionForPlacedMarketOrder(order,
                    _rabbitMqNotifyService.OrderActionCreated);

                _clientNotifyService.NotifyOrderChanged(order);
                await SendNotification(order.ClientId, order.GetPushMessage(), order);
            });
        }
    }
}