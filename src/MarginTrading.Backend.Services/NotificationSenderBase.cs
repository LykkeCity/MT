﻿using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Notifications;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Settings;
using MarginTrading.Common.Settings.Models;
using MarginTrading.Common.Settings.Repositories;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.Backend.Services
{
    public class NotificationSenderBase
    {
        private readonly IClientSettingsRepository _clientSettingsRepository;
        private readonly IAppNotifications _appNotifications;
        private readonly IClientAccountService _clientAccountService;

        public NotificationSenderBase(
            IClientSettingsRepository clientSettingsRepository,
            IAppNotifications appNotifications,
            IClientAccountService clientAccountService)
        {
            _clientSettingsRepository = clientSettingsRepository;
            _appNotifications = appNotifications;
            _clientAccountService = clientAccountService;
        }

        protected async Task SendOrderChangedNotification(string clientId, IOrder order)
        {
            var notificationType = order.Status == OrderStatus.Closed
                ? NotificationType.PositionClosed
                : NotificationType.PositionOpened;

            await SendNotification(clientId, notificationType, GetPushMessage(order),
                order.ToBackendHistoryContract());
        }
        
        protected async Task SendMarginEventNotification(string clientId, string message)
        {
            await SendNotification(clientId, NotificationType.MarginCall, message, null);
        }

        private async Task SendNotification(string clientId, NotificationType notificationType, string message,
            OrderHistoryBackendContract order)
        {
            var pushSettings = await _clientSettingsRepository.GetSettings<PushNotificationsSettings>(clientId);

            if (pushSettings != null && pushSettings.Enabled)
            {
                var notificationId = await _clientAccountService.GetNotificationId(clientId);

                await _appNotifications.SendNotification(notificationId, notificationType, message, order);
            }
        }
        
        private string GetPushMessage(IOrder order)
        {
            var message = string.Empty;
            var volume = Math.Abs(order.Volume);
            var type = order.GetOrderType() == OrderDirection.Buy ? "Long" : "Short";

            switch (order.Status)
            {
                case OrderStatus.WaitingForExecution:
                    message = string.Format(MtMessages.Notifications_PendingOrderPlaced, type, order.Instrument, volume, Math.Round(order.ExpectedOpenPrice ?? 0, order.AssetAccuracy));
                    break;
                case OrderStatus.Active:
                    message = order.ExpectedOpenPrice.HasValue
                        ? string.Format(MtMessages.Notifications_PendingOrderTriggered, order.GetOrderType() == OrderDirection.Buy ? "Long" : "Short", order.Instrument, volume,
                            Math.Round(order.OpenPrice, order.AssetAccuracy))
                        : string.Format(MtMessages.Notifications_OrderPlaced, type, order.Instrument, volume,
                            Math.Round(order.OpenPrice, order.AssetAccuracy));
                    break;
                case OrderStatus.Closed:
                    var reason = string.Empty;

                    switch (order.CloseReason)
                    {
                        case OrderCloseReason.StopLoss:
                            reason = MtMessages.Notifications_WithStopLossPhrase;
                            break;
                        case OrderCloseReason.TakeProfit:
                            reason = MtMessages.Notifications_WithTakeProfitPhrase;
                            break;
                    }

                    message = order.ExpectedOpenPrice.HasValue &&
                              (order.CloseReason == OrderCloseReason.Canceled ||
                               order.CloseReason == OrderCloseReason.CanceledBySystem)
                        ? string.Format(MtMessages.Notifications_PendingOrderCanceled, type, order.Instrument, volume)
                        : string.Format(MtMessages.Notifications_OrderClosed, type, order.Instrument, volume, reason,
                            order.GetTotalFpl().ToString($"F{MarginTradingHelpers.DefaultAssetAccuracy}"),
                            order.AccountAssetId);
                    break;
                case OrderStatus.Rejected:
                    break;
            }

            return message;
        }
    }
}