using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Settings.Models;
using MarginTrading.Common.Settings.Repositories;
using MarginTrading.Services.Notifications;

namespace MarginTrading.Services
{
    // TODO: Refactor it to pipeline and adapter
    public class SendNotificationBase
    {
        private readonly IClientSettingsRepository _clientSettingsRepository;
        private readonly IAppNotifications _appNotifications;
        private readonly IClientAccountService _clientAccountService;

        public SendNotificationBase(
            IClientSettingsRepository clientSettingsRepository,
            IAppNotifications appNotifications,
            IClientAccountService clientAccountService)
        {
            _clientSettingsRepository = clientSettingsRepository;
            _appNotifications = appNotifications;
            _clientAccountService = clientAccountService;
        }

        protected async Task SendNotification(string clientId, string message, IOrder order)
        {
            var pushSettings = await _clientSettingsRepository.GetSettings<PushNotificationsSettings>(clientId);

            if (pushSettings != null && pushSettings.Enabled)
            {
                var notificationId = await _clientAccountService.GetNotificationId(clientId);
                await _appNotifications.SendPositionNotification(new[] { notificationId }, message, order);
            }
        }
    }
}