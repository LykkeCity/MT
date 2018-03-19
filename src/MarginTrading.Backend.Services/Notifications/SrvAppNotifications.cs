using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Core.Notifications;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.Backend.Services.Notifications
{
    public class SrvAppNotifications : IAppNotifications
    {
        private readonly string _connectionString;
        private readonly string _hubName;
        private readonly ILog _log;

        public SrvAppNotifications(string connectionString, string hubName, ILog log)
        {
            _connectionString = connectionString;
            _hubName = hubName;
            _log = log;
        }

        public async Task SendNotification(string notificationsId, NotificationType notificationType, string message, OrderHistoryBackendContract order = null)
        {
            if (string.IsNullOrEmpty(notificationsId))
            {
                _log.WriteWarning(nameof(SendNotification), notificationType, "Notification id is empty");
                return;
            }
            
            await SendIosNotificationAsync(notificationsId, notificationType, message, order);
            await SendAndroidNotificationAsync(notificationsId, notificationType, message, order);
        }

        private async Task SendIosNotificationAsync(string notificationsId, NotificationType notificationType,
            string message, OrderHistoryBackendContract order = null)
        {
            var apnsMessage = new IosNotification
            {
                Aps = new IosPositionFields
                {
                    Alert = message,
                    Type = notificationType,
                    Order = order
                }
            };

            var payload = apnsMessage.ToJson(ignoreNulls: true);

            try
            {
                var hub = CustomNotificationHubClient.CreateClientFromConnectionString(_connectionString, _hubName);

                await hub.SendAppleNativeNotificationAsync(payload, new[] {notificationsId});
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(SendIosNotificationAsync), payload, e);
            }
            
            
        }

        private async Task SendAndroidNotificationAsync(string notificationsId, NotificationType notificationType, string message, OrderHistoryBackendContract order = null)
        {
            var gcmMessage = new AndroidNotification
            {
                Data = new AndroidPositionFields
                {
                    Entity = EventsAndEntities.GetEntity(notificationType),
                    Event = EventsAndEntities.GetEvent(notificationType),
                    Order = order,
                    Message = message
                }
            };
            
            var payload = gcmMessage.ToJson(ignoreNulls: true);
            
            try
            {
                var hub = CustomNotificationHubClient.CreateClientFromConnectionString(_connectionString, _hubName);

                await hub.SendGcmNativeNotificationAsync(payload, new[] {notificationsId});
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(SendAndroidNotificationAsync), payload, e);
            }
            
        }
    }
}
