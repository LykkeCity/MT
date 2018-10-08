using System.Threading.Tasks;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Service.PushNotifications.Contract;
using Lykke.Service.PushNotifications.Contract.Commands;
using Lykke.Service.PushNotifications.Contract.Enums;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.Backend.Services.Notifications
{
    public class SrvAppNotifications : IAppNotifications
    {
        private readonly ILog _log;
        private readonly ICqrsEngine _cqrsEngine;

        public SrvAppNotifications(ICqrsEngine cqrsEngine, ILog log)
        {
            _log = log;
            _cqrsEngine = cqrsEngine;
        }

        public async Task SendNotification(string notificationsId, NotificationType notificationType, string message, OrderHistoryBackendContract order = null)
        {
            if (string.IsNullOrEmpty(notificationsId))
            {
                _log.WriteWarning(nameof(SendNotification), notificationType, "Notification id is empty");
                return;
            }

            var command = new MtOrderChangedNotificationCommand
            {
                NotificationIds = new[] {notificationsId},
                Type = notificationType.ToString(),
                Message = message,
                OrderId = order?.Id
            };

            _cqrsEngine.SendCommand(command, "mt-backend", PushNotificationsBoundedContext.Name);

            await Task.CompletedTask;
        }
    }
}
