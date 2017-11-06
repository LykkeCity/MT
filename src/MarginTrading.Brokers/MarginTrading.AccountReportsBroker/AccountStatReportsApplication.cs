using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.AccountReportsBroker.Repositories;
using MarginTrading.AccountReportsBroker.Repositories.AzureRepositories.Entities;
using MarginTrading.AccountReportsBroker.Repositories.Models;
using MarginTrading.AzureRepositories;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.RabbitMqMessageModels;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AccountReportsBroker
{
    internal class AccountStatReportsApplication : BrokerApplicationBase<AccountStatsUpdateMessage>
    {
        private readonly IAccountsStatsReportsRepository _accountsStatsReportsRepository;
        private readonly IMarginTradingAccountStatsRepository _statsRepository;
        private readonly Settings _settings;

        public AccountStatReportsApplication(ILog logger,
            Settings settings, CurrentApplicationInfo applicationInfo,
            IAccountsStatsReportsRepository accountsStatsReportsRepository,
            IMarginTradingAccountStatsRepository statsRepository,
            ISlackNotificationsSender slackNotificationsSender)
            : base(logger, slackNotificationsSender, applicationInfo)
        {
            _settings = settings;
            _accountsStatsReportsRepository = accountsStatsReportsRepository;
            _statsRepository = statsRepository;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.AccountStats.ExchangeName;

        protected override Task HandleMessage(AccountStatsUpdateMessage message)
        {
            var accountsStatsReports = message.Accounts?.Select(a =>
                new AccountsStatReport
                {
                    Id = System.Guid.NewGuid().ToString().Replace("-", "").ToLower(),
                    Date = System.DateTime.UtcNow,                    
                    AccountId = a.AccountId,
                    Balance = a.Balance,
                    BaseAssetId = a.BaseAssetId,
                    ClientId = a.ClientId,
                    IsLive = a.IsLive,
                    FreeMargin = a.FreeMargin,
                    MarginAvailable = a.MarginAvailable,
                    MarginCall = a.MarginCallLevel,
                    MarginInit = a.MarginInit,
                    MarginUsageLevel = a.MarginUsageLevel,
                    OpenPositionsCount = a.OpenPositionsCount,
                    PnL = a.PnL,
                    StopOut = a.StopOutLevel,
                    TotalCapital = a.TotalCapital,
                    TradingConditionId = a.TradingConditionId,
                    UsedMargin = a.UsedMargin,
                    WithdrawTransferLimit = a.WithdrawTransferLimit
                });

            var accountStats = message.Accounts?.Select(a => new MarginTradingAccountStatsEntity
            {
                AccountId = a.AccountId,
                BaseAssetId = a.BaseAssetId,
                MarginCall = (double) a.MarginCallLevel,
                StopOut = (double) a.StopOutLevel,
                TotalCapital = (double) a.TotalCapital,
                FreeMargin = (double) a.FreeMargin,
                MarginAvailable = (double) a.MarginAvailable,
                UsedMargin = (double) a.UsedMargin,
                MarginInit = (double) a.MarginInit,
                PnL = (double) a.PnL,
                OpenPositionsCount = (double) a.OpenPositionsCount,
                MarginUsageLevel = (double) a.MarginUsageLevel,
            });

            return Task.WhenAll(
                _accountsStatsReportsRepository.InsertOrReplaceBatchAsync(accountsStatsReports),
                _statsRepository.InsertOrReplaceBatchAsync(accountStats)
            );
        }
    }
}