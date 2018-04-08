using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;
using MarginTrading.Common.Services.Client;
using MarginTrading.Common.Settings;

namespace MarginTrading.Backend.Services
{
    // TODO: Rename by role
    public class StopOutConsumer : NotificationSenderBase, IEventConsumer<StopOutEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IClientAccountService _clientAccountService;
        private readonly IClientNotifyService _notifyService;
        private readonly IEmailService _emailService;
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IDateService _dateService;
        private readonly IAssetsCache _assetsCache;

        public StopOutConsumer(IThreadSwitcher threadSwitcher,
            IAppNotifications appNotifications,
            IClientAccountService clientAccountService,
            IClientNotifyService notifyService,
            IEmailService emailService,
            IMarginTradingOperationsLogService operationsLogService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IDateService dateService,
            IAssetsCache assetsCache,
            IAssetPairsCache assetPairsCache) : base(appNotifications,
            clientAccountService,
            assetsCache, 
            assetPairsCache)
        {
            _threadSwitcher = threadSwitcher;
            _clientAccountService = clientAccountService;
            _notifyService = notifyService;
            _emailService = emailService;
            _operationsLogService = operationsLogService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _dateService = dateService;
            _assetsCache = assetsCache;
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<StopOutEventArgs>.ConsumeEvent(object sender, StopOutEventArgs ea)
        {
            var account = ea.Account;
            var orders = ea.Orders;
            var eventTime = _dateService.Now();
            var accountMarginEventMessage = AccountMarginEventMessageConverter.Create(account, true, eventTime);
            var accuracy = _assetsCache.GetAssetAccuracy(account.BaseAssetId);
            var totalPnl = Math.Round(orders.Sum(x => x.GetTotalFpl()), accuracy);

            _threadSwitcher.SwitchThread(async () =>
            {
                _operationsLogService.AddLog("stopout", account.ClientId, account.Id, "", ea.ToJson());

                var marginEventTask = _rabbitMqNotifyService.AccountMarginEvent(accountMarginEventMessage);

                _notifyService.NotifyAccountStopout(account.ClientId, account.Id, orders.Length, totalPnl);

                var notificationTask = SendMarginEventNotification(account.ClientId,
                    string.Format(MtMessages.Notifications_StopOutNotification, orders.Length, totalPnl,
                        account.BaseAssetId));

                var clientEmail = await _clientAccountService.GetEmail(account.ClientId);
                
                var emailTask = !string.IsNullOrEmpty(clientEmail)
                    ? _emailService.SendStopOutEmailAsync(clientEmail, account.BaseAssetId, account.Id)
                    : Task.CompletedTask;

                await Task.WhenAll(marginEventTask, notificationTask, emailTask);
            });
        }
    }
}