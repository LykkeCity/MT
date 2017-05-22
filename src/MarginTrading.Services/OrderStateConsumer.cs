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
		private readonly ITradingOrderService _orderActionService;

		public OrderStateConsumer(IThreadSwitcher threadSwitcher,
			IClientSettingsRepository clientSettingsRepository,
			IClientNotifyService clientNotifyService,
			IAccountsCacheService accountsCacheService,
			IAppNotifications appNotifications,
			IClientAccountService clientAccountService,
			AccountManager accountManager,
			IRabbitMqNotifyService rabbitMqNotifyService,
			ITransactionService transactionService,
			ITradingOrderService orderActionService) : base(clientSettingsRepository,
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

			    await _accountManager.UpdateBalanceAsync(order.ClientId, order.AccountId, totalFpl, AccountHistoryType.OrderClosed,
			        $"Balance changed on order close (id = {order.Id})");

				var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

				await _transactionService.CreateTransactionsForClosedOrderAsync(order,
					_rabbitMqNotifyService.TransactionCreated);

				await _orderActionService.CreateTradingOrderForClosedTakerPosition(order,
					_rabbitMqNotifyService.TradingOrderCreated);

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

				await _orderActionService.CreateTradingOrderForCancelledTakerPosition(order,
					_rabbitMqNotifyService.TradingOrderCreated);

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

				await _orderActionService.CreateTradingOrderForOpenedTakerPosition(order,
					_rabbitMqNotifyService.TradingOrderCreated);

				_clientNotifyService.NotifyOrderChanged(order);
				await SendNotification(order.ClientId, order.GetPushMessage(), order);
			});
		}
	}
}