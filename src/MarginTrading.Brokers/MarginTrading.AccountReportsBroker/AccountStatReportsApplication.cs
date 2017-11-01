using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.AccountReportsBroker.AzureRepositories;
using MarginTrading.AccountReportsBroker.AzureRepositories.Entities;
using MarginTrading.AzureRepositories;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.RabbitMqMessageModels;

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
                new AccountsStatReportEntity
                {
                    AccountId = a.AccountId,
                    Balance = (double) a.Balance,
                    BaseAssetId = a.BaseAssetId,
                    ClientId = a.ClientId,
                    IsLive = a.IsLive,
                    FreeMargin = (double) a.FreeMargin,
                    MarginAvailable = (double) a.MarginAvailable,
                    MarginCall = (double) a.MarginCallLevel,
                    MarginInit = (double) a.MarginInit,
                    MarginUsageLevel = (double) a.MarginUsageLevel,
                    OpenPositionsCount = (double) a.OpenPositionsCount,
                    PnL = (double) a.PnL,
                    StopOut = (double) a.StopOutLevel,
                    TotalCapital = (double) a.TotalCapital,
                    TradingConditionId = a.TradingConditionId,
                    UsedMargin = (double) a.UsedMargin,
                    WithdrawTransferLimit = (double) a.WithdrawTransferLimit
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