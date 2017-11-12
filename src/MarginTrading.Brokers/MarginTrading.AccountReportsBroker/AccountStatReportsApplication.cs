using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.AccountReportsBroker.Repositories;
using MarginTrading.AccountReportsBroker.Repositories.Models;
using MarginTrading.AzureRepositories;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.RabbitMqMessageModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AccountReportsBroker
{
    internal abstract class AccountStatReportsApplication : BrokerApplicationBase<AccountStatsUpdateMessage>
    {
        private readonly IAccountsStatsReportsRepository _accountsStatsReportsRepository;
        private readonly IMarginTradingAccountStatsRepository _statsRepository;
        private readonly Settings _settings;
        private const int DecimalPlaces = 10;

        protected AccountStatReportsApplication(ILog logger,
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
                    Id = a.AccountId,
                    Date = DateTime.UtcNow,
                    AccountId = a.AccountId,
                    Balance = Math.Round(a.Balance, DecimalPlaces),
                    BaseAssetId = a.BaseAssetId,
                    ClientId = a.ClientId,
                    IsLive = a.IsLive,
                    FreeMargin = Math.Round(a.FreeMargin, DecimalPlaces),
                    MarginAvailable = Math.Round(a.MarginAvailable, DecimalPlaces),
                    MarginCall = Math.Round(a.MarginCallLevel, DecimalPlaces),
                    MarginInit = Math.Round(a.MarginInit, DecimalPlaces),
                    MarginUsageLevel = Math.Round(a.MarginUsageLevel, DecimalPlaces),
                    OpenPositionsCount = Math.Round(a.OpenPositionsCount, DecimalPlaces),
                    PnL = Math.Round(a.PnL, DecimalPlaces),
                    StopOut = Math.Round(a.StopOutLevel, DecimalPlaces),
                    TotalCapital = Math.Round(a.TotalCapital, DecimalPlaces),
                    TradingConditionId = a.TradingConditionId,
                    UsedMargin = Math.Round(a.UsedMargin, DecimalPlaces),
                    WithdrawTransferLimit = Math.Round(a.WithdrawTransferLimit, DecimalPlaces)
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