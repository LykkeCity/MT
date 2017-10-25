using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Services.Events;
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

		public OrderStateConsumer(IThreadSwitcher threadSwitcher,
			IClientSettingsRepository clientSettingsRepository,
			IClientNotifyService clientNotifyService,
			IAccountsCacheService accountsCacheService,
			IAppNotifications appNotifications,
			IClientAccountService clientAccountService,
			AccountManager accountManager,
			IRabbitMqNotifyService rabbitMqNotifyService) : base(clientSettingsRepository,
			appNotifications,
			clientAccountService)
		{
			_threadSwitcher = threadSwitcher;
			_clientNotifyService = clientNotifyService;
			_accountsCacheService = accountsCacheService;
			_accountManager = accountManager;
			_rabbitMqNotifyService = rabbitMqNotifyService;
		}

		int IEventConsumer.ConsumerRank => 100;

		void IEventConsumer<OrderClosedEventArgs>.ConsumeEvent(object sender, OrderClosedEventArgs ea)
		{
			var order = ea.Order;
			_threadSwitcher.SwitchThread(async () =>
			{
				await _rabbitMqNotifyService.OrderHistory(order);
				_clientNotifyService.NotifyOrderChanged(order);
				
				var totalFpl = order.GetTotalFpl();
			    await _accountManager.UpdateBalanceAsync(order.ClientId, order.AccountId, totalFpl, AccountHistoryType.OrderClosed,
			        $"Balance changed on order close (id = {order.Id})");

				await SendNotification(order.ClientId, order.GetPushMessage(), order);
			});
		}

		void IEventConsumer<OrderCancelledEventArgs>.ConsumeEvent(object sender, OrderCancelledEventArgs ea)
		{
			var order = ea.Order;
			_threadSwitcher.SwitchThread(async () =>
			{
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
				_clientNotifyService.NotifyOrderChanged(order);
				await SendNotification(order.ClientId, order.GetPushMessage(), order);
			});
		}
	}
}