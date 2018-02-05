using System.Threading.Tasks;
using MarginTrading.Backend.Core.Notifications;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.Backend.Services.Notifications
{
    public interface IAppNotifications
    {
        Task SendNotification(string notificationId, NotificationType notificationType, string message,
            OrderHistoryBackendContract order);
    }
}