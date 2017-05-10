using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Messages;
using MarginTrading.Services.Events;
using MarginTrading.Services.Generated.ClientAccountServiceApi;
using MarginTrading.Services.Generated.ClientAccountServiceApi.Models;
using MarginTrading.Services.Notifications;

namespace MarginTrading.Services
{
    // TODO: Rename by role
    public class MarginCallConsumer : SendNotificationBase, IEventConsumer<MarginCallEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IEmailService _emailService;
        private readonly IClientAccountService _clientAccountService;

        public MarginCallConsumer(IThreadSwitcher threadSwitcher,
            IClientSettingsRepository clientSettingsRepository,
            IAppNotifications appNotifications,
            IEmailService emailService,
            IClientAccountService clientAccountService) : base(clientSettingsRepository,
            appNotifications, clientAccountService)
        {
            _threadSwitcher = threadSwitcher;
            _emailService = emailService;
            _clientAccountService = clientAccountService;
        }

        int IEventConsumer.ConsumerRank => 100;
        void IEventConsumer<MarginCallEventArgs>.ConsumeEvent(object sender, MarginCallEventArgs ea)
        {
            var account = ea.Account;
            _threadSwitcher.SwitchThread(async () =>
            {
                await SendNotification(account.ClientId, string.Format(MtMessages.Notifications_MarginCall, account.GetMarginUsageLevel(),
                        account.BaseAssetId), null);

                var clientAcc = await _clientAccountService.ApiClientAccountsGetByIdPostAsync(new GetByIdRequest(account.ClientId));

                if (clientAcc != null)
                    await _emailService.SendMarginCallEmailAsync(clientAcc.Email, account.BaseAssetId, account.Id);
            });
        }
    }
}