using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.Common.Services;

namespace MarginTrading.AzureRepositories
{
    public class AzureRepoFactories
    {
        public static class MarginTrading
        {

            public static MarginTradingAccountHistoryRepository CreateAccountHistoryRepository(IReloadingManager<string> connString, ILog log)
            {
                return new MarginTradingAccountHistoryRepository(AzureTableStorage<MarginTradingAccountHistoryEntity>.Create(connString,
                    "AccountsHistory", log));
            }

            public static MarginTradingAccountStatsRepository CreateAccountStatsRepository(IReloadingManager<string> connString, ILog log)
            {
                return new MarginTradingAccountStatsRepository(AzureTableStorage<MarginTradingAccountStatsEntity>.Create(connString,
                    "MarginTradingAccountStats", log));
            }

            public static MarginTradingBlobRepository CreateBlobRepository(IReloadingManager<string> connString)
            {
                return new MarginTradingBlobRepository(connString);
            }

            public static RiskSystemCommandsLogRepository CreateRiskSystemCommandsLogRepository(IReloadingManager<string> connString, ILog log)
            {
                return new RiskSystemCommandsLogRepository(AzureTableStorage<RiskSystemCommandsLogEntity>.Create(connString,
                    "RiskSystemCommandsLog", log));
            }
        }
    }
}