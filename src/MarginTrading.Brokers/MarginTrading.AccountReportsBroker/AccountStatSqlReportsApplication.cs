using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.AccountReportsBroker.Repositories;
using MarginTrading.AzureRepositories;
using MarginTrading.BrokerBase.Settings;

namespace MarginTrading.AccountReportsBroker
{
    internal class AccountStatSqlReportsApplication : AccountStatReportsApplication
    {
        public AccountStatSqlReportsApplication(ILog logger, Settings settings,
            CurrentApplicationInfo applicationInfo, IAccountsStatsReportsRepository accountsStatsReportsRepository,
            IMarginTradingAccountStatsRepository statsRepository, ISlackNotificationsSender slackNotificationsSender) :
            base(logger, settings, applicationInfo, accountsStatsReportsRepository, statsRepository,
                slackNotificationsSender)
        {
        }

        protected override string QueuePostfix => ".Sql";
    }
}