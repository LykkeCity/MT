using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Common;
using Lykke.Service.TemplateFormatter.Client;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;
using MarginTrading.Common.Services.Client;

namespace MarginTrading.Backend.Services
{
    // TODO: Rename by role
    public class StopOutConsumer : NotificationSenderBase, IEventConsumer<StopOutEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IClientNotifyService _notifyService;
        private readonly IEmailService _emailService;
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IDateService _dateService;
        private readonly IAssetsCache _assetsCache;

        public StopOutConsumer(IThreadSwitcher threadSwitcher,
            IAppNotifications appNotifications,
            IClientAccountService clientAccountService,
            ITemplateFormatter templateFormatter,
            IClientNotifyService notifyService,
            IEmailService emailService,
            IMarginTradingOperationsLogService operationsLogService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IDateService dateService,
            IAssetsCache assetsCache,
            IAssetPairsCache assetPairsCache) : base(appNotifications,
            clientAccountService,
            templateFormatter,
            assetsCache, 
            assetPairsCache)
        {
            _threadSwitcher = threadSwitcher;
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

                var notificationTask = SendStopOutNotification(account.ClientId, orders.Length, totalPnl, account.BaseAssetId);

                var emailTask = _emailService.SendStopOutEmailAsync(account);

                await Task.WhenAll(marginEventTask, notificationTask, emailTask);
            });
        }
    }
}
