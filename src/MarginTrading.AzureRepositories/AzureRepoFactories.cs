using AzureStorage.Queue;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common.Log;
using MarginTrading.AzureRepositories.Clients;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.AzureRepositories.Reports;
using MarginTrading.AzureRepositories.Settings;

namespace MarginTrading.AzureRepositories
{
    public class AzureRepoFactories
    {
        public static class Clients
        {
            public static ClientSettingsRepository CreateTraderSettingsRepository(string connString, ILog log)
            {
                return new ClientSettingsRepository(
                    AzureTableStorage<ClientSettingsEntity>.Create(() => connString, "TraderSettings", log));
            }

            public static ClientsRepository CreateClientsRepository(string connString, ILog log)
            {
                const string tableName = "Traders";
                return new ClientsRepository(
                    AzureTableStorage<ClientAccountEntity>.Create(() => connString, tableName, log),
                    AzureTableStorage<AzureIndex>.Create(() => connString, tableName, log));
            }
        }

        public static class Settings
        {
            public static AppGlobalSettingsRepository CreateAppGlobalSettingsRepository(string connstring, ILog log)
            {
                return new AppGlobalSettingsRepository(AzureTableStorage<AppGlobalSettingsEntity>.Create(() => connstring, "Setup", log));
            }
        }

        public static class MarginTrading
        {
            public static MarginTradingConditionsRepository CreateTradingConditionsRepository(string connstring, ILog log)
            {
                return new MarginTradingConditionsRepository(AzureTableStorage<MarginTradingConditionEntity>.Create(() => connstring,
                    "MarginTradingConditions", log));
            }

            public static MarginTradingAccountGroupRepository CreateAccountGroupRepository(string connstring, ILog log)
            {
                return new MarginTradingAccountGroupRepository(AzureTableStorage<MarginTradingAccountGroupEntity>.Create(() => connstring,
                    "MarginTradingAccountGroups", log));
            }

            public static AccountAssetsPairsRepository CreateAccountAssetsRepository(string connstring, ILog log)
            {
                return new AccountAssetsPairsRepository(AzureTableStorage<AccountAssetPairEntity>.Create(() => connstring,
                    "MarginTradingAccountAssets", log));
            }

            public static AssetPairsRepository CreateAssetsRepository(string connstring, ILog log)
            {
                return new AssetPairsRepository(AzureTableStorage<AssetPairEntity>.Create(() => connstring,
                    "MarginTradingAssets", log));
            }

            public static MarginTradingOrdersHistoryRepository CreateOrdersHistoryRepository(string connstring, ILog log)
            {
                return new MarginTradingOrdersHistoryRepository(AzureTableStorage<MarginTradingOrderHistoryEntity>.Create(() => connstring,
                    "MarginTradingOrdersHistory", log));
            }

            public static MarginTradingOrdersRejectedRepository CreateOrdersRejectedRepository(string connstring, ILog log)
            {
                return new MarginTradingOrdersRejectedRepository(AzureTableStorage<MarginTradingOrderRejectedEntity>.Create(() => connstring,
                    "MarginTradingOrdersRejected", log));
            }

            public static MarginTradingAccountHistoryRepository CreateAccountHistoryRepository(string connstring, ILog log)
            {
                return new MarginTradingAccountHistoryRepository(AzureTableStorage<MarginTradingAccountHistoryEntity>.Create(() => connstring,
                    "AccountsHistory", log));
            }

            public static MarginTradingAccountsRepository CreateAccountsRepository(string connstring, ILog log)
            {
                return new MarginTradingAccountsRepository(AzureTableStorage<MarginTradingAccountEntity>.Create(() => connstring,
                    "MarginTradingAccounts", log));
            }

            public static MarginTradingAccountStatsRepository CreateAccountStatsRepository(string connstring, ILog log)
            {
                return new MarginTradingAccountStatsRepository(AzureTableStorage<MarginTradingAccountStatsEntity>.Create(() => connstring,
                    "MarginTradingAccountStats", log));
            }

            public static MarginTradingBlobRepository CreateBlobRepository(string connstring)
            {
                return new MarginTradingBlobRepository(connstring);
            }

            public static MarginTradingWatchListsRepository CreateWatchListsRepository(string connstring, ILog log)
            {
                return new MarginTradingWatchListsRepository(AzureTableStorage<MarginTradingWatchListEntity>.Create(() => connstring,
                    "MarginTradingWatchLists", log));
            }

            public static MatchingEngineRoutesRepository CreateMatchingEngineRoutesRepository(string connstring, ILog log)
            {
                return new MatchingEngineRoutesRepository(AzureTableStorage<MatchingEngineRouteEntity>.Create(() => connstring,
                    "MatchingEngineRoutes", log));
            }

            public static AccountsStatsReportsRepository CreateAccountsStatsReportsRepository(string connstring, ILog log)
            {
                return new AccountsStatsReportsRepository(AzureTableStorage<AccountsStatReportEntity>.Create(() => connstring,
                    "ClientAccountsStatusReports", log));
            }

            public static AccountsReportsRepository CreateAccountsReportsRepository(string connstring, ILog log)
            {
                return new AccountsReportsRepository(AzureTableStorage<AccountsReportEntity>.Create(() => connstring,
                    "ClientAccountsReports", log));
            }
            
            public static RiskSystemCommandsLogRepository CreateRiskSystemCommandsLogRepository(string connstring, ILog log)
            {
                return new RiskSystemCommandsLogRepository(AzureTableStorage<RiskSystemCommandsLogEntity>.Create(() => connstring,
                    "RiskSystemCommandsLog", log));
            }
        }
    }
}