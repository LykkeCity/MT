using System;
using System.Threading.Tasks;
using Lykke.Service.EmailSender;
using Lykke.Service.PushNotifications.Contract.Enums;
using Lykke.Service.TemplateFormatter.Client;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
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
        private readonly ITemplateFormatter _templateFormatter;
        private readonly IAssetsCache _assetsCache;
        private readonly IAssetPairsCache _assetPairsCache;

        public NotificationSenderBase(
            IAppNotifications appNotifications,
            IClientAccountService clientAccountService,
            ITemplateFormatter templateFormatter,
            IAssetsCache assetsCache,
            IAssetPairsCache assetPairsCache)
        {
            _appNotifications = appNotifications;
            _clientAccountService = clientAccountService;
            _templateFormatter = templateFormatter;
            _assetsCache = assetsCache;
            _assetPairsCache = assetPairsCache;
        }

        protected async Task SendOrderChangedNotification(string clientId, IOrder order)
        {
            var notificationType = order.Status == OrderStatus.Closed
                ? NotificationType.PositionClosed
                : NotificationType.PositionOpened;

            var clientAcc = await _clientAccountService.GetClientAsync(clientId);

            if (clientAcc != null && !string.IsNullOrEmpty(clientAcc.NotificationsId))
            {
                string message = await GetOrderChangedPushMessageAsync(order, clientAcc.PartnerId);

                if (!string.IsNullOrEmpty(message))
                    await SendNotification(clientId, clientAcc.NotificationsId, notificationType, message, order.ToBackendHistoryContract());
            }
        }
        
        protected async Task SendStopOutNotification(string clientId, int count, decimal totalPnl, string baseAssetId)
        {
            var clientAcc = await _clientAccountService.GetClientAsync(clientId);

            if (clientAcc != null && !string.IsNullOrEmpty(clientAcc.NotificationsId))
            {
                var message = await _templateFormatter.FormatAsync("PushMtStopOutTemplate", clientAcc.PartnerId,
                    "EN", new
                    {
                        Count = count,
                        TotalPnl = totalPnl,
                        AssetId = baseAssetId
                    });

                if (message != null)
                    await SendNotification(clientId, clientAcc.NotificationsId, NotificationType.MarginCall, message.Subject, null);
            }
        }
        
        protected async Task SendMarginCallNotification(string clientId, decimal marginUsed, string baseAssetId)
        {
            var clientAcc = await _clientAccountService.GetClientAsync(clientId);

            if (clientAcc != null && !string.IsNullOrEmpty(clientAcc.NotificationsId))
            {
                var message = await _templateFormatter.FormatAsync("PushMtMarginUsedTemplate", clientAcc.PartnerId,
                    "EN", new
                    {
                        MarginUsed = $"{marginUsed:P}",
                        AssetId = baseAssetId
                    });

                if (message != null)
                    await SendNotification(clientId, clientAcc.NotificationsId, NotificationType.MarginCall, message.Subject, null);
            }
        }

        private async Task SendNotification(string clientId, string notificationId, NotificationType notificationType, string message,
            OrderHistoryBackendContract order)
        {
            if (await _clientAccountService.IsPushEnabled(clientId))
            {
                await _appNotifications.SendNotification(notificationId, notificationType, message, order);
            }
        }
        
        private async Task<string> GetOrderChangedPushMessageAsync(IOrder order, string partnetId)
        {
            EmailMessage message = null;
            var volume = Math.Abs(order.Volume);
            var type = order.GetOrderType() == OrderDirection.Buy ? "Long" : "Short";
            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(order.Instrument);
            var instrumentName = assetPair?.Name ?? order.Instrument;

            string templateName = null;
            object model = null;
            
            switch (order.Status)
            {
                case OrderStatus.WaitingForExecution:
                    templateName = "PushMtPendingOrderPlacedTemplate";
                    model = new
                    {
                        OrderType = type,
                        AssetPairId = instrumentName,
                        Volume = volume,
                        Price = Math.Round(order.ExpectedOpenPrice ?? 0, order.AssetAccuracy)
                    };
                    break;
                case OrderStatus.Active:
                    templateName = order.ExpectedOpenPrice.HasValue
                        ? "PushMtPendingOrderTriggeredTemplate"
                        : "PushMtPendingOrderPlacedTemplate";
                    
                    model = new
                    {
                        OrderType = type,
                        AssetPairId = instrumentName,
                        Volume = volume,
                        Price = Math.Round(order.ExpectedOpenPrice ?? 0, order.AssetAccuracy)
                    };
                    break;
                case OrderStatus.Closed:
                    templateName = "PushMtOrderClosedTemplate";

                    switch (order.CloseReason)
                    {
                        case OrderCloseReason.StopLoss:
                            templateName = "PushMtOrderClosedStopLossTemplate";
                            break;
                        case OrderCloseReason.TakeProfit:
                            templateName = "PushMtOrderClosedTakeProfitTemplate";
                            break;
                    }

                    var accuracy = _assetsCache.GetAssetAccuracy(order.AccountAssetId);

                    if (order.ExpectedOpenPrice.HasValue &&
                        (order.CloseReason == OrderCloseReason.Canceled ||
                         order.CloseReason == OrderCloseReason.CanceledBySystem ||
                         order.CloseReason == OrderCloseReason.CanceledByBroker))
                    {
                        templateName = "PushMtOrderCanceledTemplate";
                        
                        model = new
                        {
                            OrderType = type,
                            AssetPairId = instrumentName,
                            Volume = volume,
                            TotalPnl = string.Empty,
                            AssetId = string.Empty
                        };
                    }
                    else
                    {
                        model = new
                        {
                            OrderType = type,
                            AssetPairId = instrumentName,
                            Volume = volume,
                            TotalPnl = order.GetTotalFpl().ToString($"F{accuracy}"),
                            AssetId = order.AccountAssetId
                        };
                    }
                    break;
                case OrderStatus.Rejected:
                    break;
                case OrderStatus.Closing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (!string.IsNullOrEmpty(templateName) && model != null)
                message = await _templateFormatter.FormatAsync(templateName, partnetId, "EN", model);

            return message?.Subject;
        }
    }
}
