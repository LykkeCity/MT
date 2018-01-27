using Lykke.Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Settings;
using MarginTrading.Common.Settings.Repositories;

namespace MarginTrading.Backend.Services.EventsConsumers
{
	// TODO: Rename by role
	public class OrderStateConsumer : NotificationSenderBase,
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
			IRabbitMqNotifyService rabbitMqNotifyService,
			IAssetsCache assetsCache)
			: base(clientSettingsRepository,
				appNotifications,
				clientAccountService,
				assetsCache)
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
				var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
				await _accountManager.UpdateBalanceAsync(account, totalFpl, AccountHistoryType.OrderClosed,
					$"Balance changed on order close (id = {order.Id})", order.Id);

				await SendOrderChangedNotification(order.ClientId, order);
			});
		}

		void IEventConsumer<OrderCancelledEventArgs>.ConsumeEvent(object sender, OrderCancelledEventArgs ea)
		{
			var order = ea.Order;
			_threadSwitcher.SwitchThread(async () =>
			{
				_clientNotifyService.NotifyOrderChanged(order);
				await _rabbitMqNotifyService.OrderHistory(order);
				await SendOrderChangedNotification(order.ClientId, order);
			});
		}

		void IEventConsumer<OrderPlacedEventArgs>.ConsumeEvent(object sender, OrderPlacedEventArgs ea)
		{
			var order = ea.Order;

			_threadSwitcher.SwitchThread(async () =>
			{
				_clientNotifyService.NotifyOrderChanged(order);
				await SendOrderChangedNotification(order.ClientId, order);
			});
		}
	}
}