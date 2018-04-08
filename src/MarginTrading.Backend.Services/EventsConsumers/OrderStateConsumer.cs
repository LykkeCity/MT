using System;
using System.Threading.Tasks;
using Lykke.Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services.Client;
using MarginTrading.Common.Settings;

namespace MarginTrading.Backend.Services.EventsConsumers
{
	public class OrderStateConsumer : NotificationSenderBase,
		IEventConsumer<OrderUpdateBaseEventArgs>
	{
		private readonly IThreadSwitcher _threadSwitcher;
		private readonly IClientNotifyService _clientNotifyService;
		private readonly IAccountsCacheService _accountsCacheService;
		private readonly AccountManager _accountManager;
		private readonly IRabbitMqNotifyService _rabbitMqNotifyService;

		public OrderStateConsumer(IThreadSwitcher threadSwitcher,
			IClientNotifyService clientNotifyService,
			IAccountsCacheService accountsCacheService,
			IAppNotifications appNotifications,
			IClientAccountService clientAccountService,
			AccountManager accountManager,
			IRabbitMqNotifyService rabbitMqNotifyService,
			IAssetsCache assetsCache,
			IAssetPairsCache assetPairsCache)
			: base(appNotifications,
				clientAccountService,
				assetsCache,
				assetPairsCache)
		{
			_threadSwitcher = threadSwitcher;
			_clientNotifyService = clientNotifyService;
			_accountsCacheService = accountsCacheService;
			_accountManager = accountManager;
			_rabbitMqNotifyService = rabbitMqNotifyService;
		}

		void IEventConsumer<OrderUpdateBaseEventArgs>.ConsumeEvent(object sender, OrderUpdateBaseEventArgs ea)
		{
			SendOrderHistory(ea);
			switch (ea.UpdateType)
			{
				case OrderUpdateType.Closing:
					break;
				case OrderUpdateType.Activate:
				case OrderUpdateType.Place:
					OnPlacedOrActivated(ea);
					break;
				case OrderUpdateType.Cancel:
					OnCancelled(ea);
					break;
				case OrderUpdateType.Reject:
					OnRejected(ea);
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

		private void OnRejected(OrderUpdateBaseEventArgs ea)
		{
			_rabbitMqNotifyService.OrderReject(ea.Order);
		}

		private void OnClosed(OrderUpdateBaseEventArgs ea)
		{
			var order = ea.Order;
			_threadSwitcher.SwitchThread(async () =>
			{
				_clientNotifyService.NotifyOrderChanged(order);
				
				var totalFpl = order.GetTotalFpl();
				var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
				await _accountManager.UpdateBalanceAsync(account, totalFpl, AccountHistoryType.OrderClosed,
					$"Balance changed on order close (id = {order.Id})", order.Id);

				await SendOrderChangedNotification(order.ClientId, order);
			});
		}

		private void OnCancelled(OrderUpdateBaseEventArgs ea)
		{
			var order = ea.Order;
			_threadSwitcher.SwitchThread(async () =>
			{
				_clientNotifyService.NotifyOrderChanged(order);
				await SendOrderChangedNotification(order.ClientId, order);
			});
		}

		private void OnPlacedOrActivated(OrderUpdateBaseEventArgs ea)
		{
			var order = ea.Order;
			_threadSwitcher.SwitchThread(async () =>
			{
				_clientNotifyService.NotifyOrderChanged(order);
				await SendOrderChangedNotification(order.ClientId, order);
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