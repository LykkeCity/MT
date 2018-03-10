using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.AccountReportsBroker.Repositories;
using MarginTrading.AccountReportsBroker.Repositories.Models;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.RabbitMqMessageModels;
using System;
using System.Threading.Tasks;

namespace MarginTrading.AccountReportsBroker
{
    internal class AccountReportsApplication : BrokerApplicationBase<AccountChangedMessage>
    {
        private readonly IAccountsReportsRepository _accountsReportsRepository;
        private readonly Settings _settings;

        public AccountReportsApplication(ILog logger,
            Settings settings, CurrentApplicationInfo applicationInfo,
            IAccountsReportsRepository accountsReportsRepository,
            ISlackNotificationsSender slackNotificationsSender)
            : base(logger, slackNotificationsSender, applicationInfo)
        {
            _settings = settings;
            _accountsReportsRepository = accountsReportsRepository;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.AccountChanged.ExchangeName;

        protected override Task HandleMessage(AccountChangedMessage message)
        {
            if (message.EventType != AccountEventTypeEnum.Created || message.Account == null)
            {
                return Task.CompletedTask;
            }

            var report = new AccountsReport
            {
                Id = message.Account.Id,
                Date = DateTime.UtcNow,
                TakerAccountId = message.Account.Id,
                TakerCounterpartyId = message.Account.ClientId,
                BaseAssetId = message.Account.BaseAssetId,
                IsLive = _settings.IsLive,
                LegalEntity = message.Account.LegalEntity,
            };

            return _accountsReportsRepository.InsertOrReplaceAsync(report);
        }
    }
}