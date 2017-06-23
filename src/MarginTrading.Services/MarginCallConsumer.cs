using Common;
using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Messages;
using MarginTrading.Services.Events;
using MarginTrading.Services.Notifications;

namespace MarginTrading.Services
{
    // TODO: Rename by role
    public class MarginCallConsumer : SendNotificationBase, IEventConsumer<MarginCallEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IEmailService _emailService;
        private readonly IClientAccountService _clientAccountService;
        private readonly IMarginTradingOperationsLogService _operationsLogService;

        public MarginCallConsumer(IThreadSwitcher threadSwitcher,
            IClientSettingsRepository clientSettingsRepository,
            IAppNotifications appNotifications,
            IEmailService emailService,
            IClientAccountService clientAccountService,
            IMarginTradingOperationsLogService operationsLogService) : base(clientSettingsRepository,
            appNotifications, clientAccountService)
        {
            _threadSwitcher = threadSwitcher;
            _emailService = emailService;
            _clientAccountService = clientAccountService;
            _operationsLogService = operationsLogService;
        }

        int IEventConsumer.ConsumerRank => 100;
        void IEventConsumer<MarginCallEventArgs>.ConsumeEvent(object sender, MarginCallEventArgs ea)
        {
            var account = ea.Account;
            _threadSwitcher.SwitchThread(async () =>
            {
                _operationsLogService.AddLog("margin call", account.ClientId, account.Id, "", ea.ToJson());

                await SendNotification(account.ClientId, string.Format(MtMessages.Notifications_MarginCall, account.GetMarginUsageLevel(),
                        account.BaseAssetId), null);

                var clientAcc = await _clientAccountService.GetAsync(account.ClientId);

                if (clientAcc != null)
                    await _emailService.SendMarginCallEmailAsync(clientAcc.Email, account.BaseAssetId, account.Id);
            });
        }
    }
}