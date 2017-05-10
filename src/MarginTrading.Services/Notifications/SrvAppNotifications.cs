using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;
using MarginTrading.Core.Notifications;
using Newtonsoft.Json;

namespace MarginTrading.Services.Notifications
{
    public interface IAppNotifications
    {
        Task SendPositionNotification(string[] notificationIds, string message, IOrder order);
    }

    public enum Device
    {
        Android, Ios
    }

    public interface IIosNotification { }

    public interface IAndroidNotification { }

    public class IosFields
    {
        [JsonProperty("alert")]
        public string Alert { get; set; }
        [JsonProperty("type")]
        public NotificationType Type { get; set; }
    }

    public class AndroidPayloadFields
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("entity")]
        public string Entity { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class IosNotification : IIosNotification
    {
        [JsonProperty("aps")]
        public IosFields Aps { get; set; }
    }

    public class AndoridPayloadNotification : IAndroidNotification
    {
        [JsonProperty("data")]
        public AndroidPayloadFields Data { get; set; }
    }

    public class PositionFieldsIos : IosFields
    {
        [JsonProperty("order")]
        public OrderHistoryBackendContract Order { get; set; }
    }

    public class PositionFieldsAndroid : AndroidPayloadFields
    {
        [JsonProperty("order")]
        public OrderHistoryBackendContract Order { get; set; }
    }

    public class SrvAppNotifications : IAppNotifications
    {
        private readonly string _connectionString;
        private readonly string _hubName;

        public SrvAppNotifications(string connectionString, string hubName)
        {
            _connectionString = connectionString;
            _hubName = hubName;
        }

        public async Task SendPositionNotification(string[] notificationsIds, string message, IOrder order)
        {
            NotificationType notificationType;
            OrderHistoryBackendContract orderContract = null;

            if (order == null)
            {
                notificationType = NotificationType.MarginCall;
            }
            else
            {
                notificationType = order.Status == OrderStatus.Closed
                   ? NotificationType.PositionClosed
                   : NotificationType.PositionOpened;

                orderContract = order.ToBackendHistoryContract();
            }

            var apnsMessage = new IosNotification
            {
                Aps = new PositionFieldsIos
                {
                    Alert = message,
                    Type = notificationType,
                    Order = orderContract
                }
            };

            var gcmMessage = new AndoridPayloadNotification
            {
                Data = new PositionFieldsAndroid
                {
                    Entity = EventsAndEntities.GetEntity(notificationType),
                    Event = EventsAndEntities.GetEvent(notificationType),
                    Order = orderContract,
                    Message = message
                }
            };

            await SendIosNotificationAsync(notificationsIds, apnsMessage);
            await SendAndroidNotificationAsync(notificationsIds, gcmMessage);
        }

        private async Task SendIosNotificationAsync(string[] notificationIds, IIosNotification notification)
        {
            await SendRawNotificationAsync(Device.Ios, notificationIds, notification.ToJson(ignoreNulls: true));
        }

        private async Task SendAndroidNotificationAsync(string[] notificationIds, IAndroidNotification notification)
        {
            await SendRawNotificationAsync(Device.Android, notificationIds, notification.ToJson(ignoreNulls: true));
        }

        private async Task SendRawNotificationAsync(Device device, string[] notificationIds, string payload)
        {
            try
            {
                notificationIds = notificationIds?.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (notificationIds != null && notificationIds.Any())
                {
                    var hub = CustomNotificationHubClient.CreateClientFromConnectionString(_connectionString, _hubName);

                    if (device == Device.Ios)
                        await hub.SendAppleNativeNotificationAsync(payload, notificationIds);
                    else
                        await hub.SendGcmNativeNotificationAsync(payload, notificationIds);
                }
            }
            catch (Exception)
            {
                //TODO: process exception
            }
        }
    }
}
