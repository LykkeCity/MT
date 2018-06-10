using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Notifications;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services.Client;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.Backend.Services
{
    public class NotificationSenderBase
    {
        private readonly IAppNotifications _appNotifications;
        private readonly IClientAccountService _clientAccountService;
        private readonly IAssetsCache _assetsCache;
        private readonly IAssetPairsCache _assetPairsCache;

        public NotificationSenderBase(
            IAppNotifications appNotifications,
            IClientAccountService clientAccountService,
            IAssetsCache assetsCache,
            IAssetPairsCache assetPairsCache)
        {
            _appNotifications = appNotifications;
            _clientAccountService = clientAccountService;
            _assetsCache = assetsCache;
            _assetPairsCache = assetPairsCache;
        }

        protected async Task SendOrderChangedNotification(string clientId, IPosition order)
        {
            var notificationType = order.Status == PositionStatus.Closed
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
            if (await _clientAccountService.IsPushEnabled(clientId))
            {
                var notificationId = await _clientAccountService.GetNotificationId(clientId);

                await _appNotifications.SendNotification(notificationId, notificationType, message, order);
            }
        }
        
        private string GetPushMessage(IPosition order)
        {
            var message = string.Empty;
            var volume = Math.Abs(order.Volume);
            var type = order.GetOrderDirection() == OrderDirection.Buy ? "Long" : "Short";
            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(order.Instrument);
            var instrumentName = assetPair?.Name ?? order.Instrument;
            
            switch (order.Status)
            {
                case PositionStatus.WaitingForExecution:
                    message = string.Format(MtMessages.Notifications_PendingOrderPlaced, type, instrumentName, volume, Math.Round(order.ExpectedOpenPrice ?? 0, order.AssetAccuracy));
                    break;
                case PositionStatus.Active:
                    message = order.ExpectedOpenPrice.HasValue
                        ? string.Format(MtMessages.Notifications_PendingOrderTriggered, type, instrumentName, volume,
                            Math.Round(order.OpenPrice, order.AssetAccuracy))
                        : string.Format(MtMessages.Notifications_OrderPlaced, type, instrumentName, volume,
                            Math.Round(order.OpenPrice, order.AssetAccuracy));
                    break;
                case PositionStatus.Closed:
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

                    var accuracy = _assetsCache.GetAssetAccuracy(order.AccountAssetId);

                    message = order.ExpectedOpenPrice.HasValue &&
                              (order.CloseReason == OrderCloseReason.Canceled ||
                               order.CloseReason == OrderCloseReason.CanceledBySystem ||
                               order.CloseReason == OrderCloseReason.CanceledByBroker)
                        ? string.Format(MtMessages.Notifications_PendingOrderCanceled, type, instrumentName, volume)
                        : string.Format(MtMessages.Notifications_OrderClosed, type, instrumentName, volume, reason,
                            order.GetTotalFpl().ToString($"F{accuracy}"),
                            order.AccountAssetId);
                    break;
                case PositionStatus.Rejected:
                    break;
                case PositionStatus.Closing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return message;
        }
    }
}