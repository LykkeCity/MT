using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.AzureRepositories.Entities;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services;

namespace MarginTrading.AzureRepositories
{
    public class AzureRepoFactories
    {
        public static class MarginTrading
        {
            public static TradingConditionsRepository CreateTradingConditionsRepository(IReloadingManager<string> connString, ILog log)
            {
                return new TradingConditionsRepository(AzureTableStorage<TradingConditionEntity>.Create(connString,
                    "MarginTradingConditions", log));
            }

            public static AccountGroupRepository CreateAccountGroupRepository(IReloadingManager<string> connString, ILog log)
            {
                return new AccountGroupRepository(AzureTableStorage<AccountGroupEntity>.Create(connString,
                    "MarginTradingAccountGroups", log));
            }

            public static AccountAssetsPairsRepository CreateAccountAssetsRepository(IReloadingManager<string> connString, ILog log)
            {
                return new AccountAssetsPairsRepository(AzureTableStorage<AccountAssetPairEntity>.Create(connString,
                    "MarginTradingAccountAssets", log));
            }

            public static MarginTradingOrdersHistoryRepository CreateOrdersHistoryRepository(IReloadingManager<string> connString, ILog log)
            {
                return new MarginTradingOrdersHistoryRepository(AzureTableStorage<MarginTradingOrderHistoryEntity>.Create(connString,
                    "MarginTradingOrdersHistory", log));
            }

            public static MarginTradingOrdersRejectedRepository CreateOrdersRejectedRepository(IReloadingManager<string> connString, ILog log)
            {
                return new MarginTradingOrdersRejectedRepository(AzureTableStorage<MarginTradingOrderRejectedEntity>.Create(connString,
                    "MarginTradingOrdersRejected", log));
            }

            public static MarginTradingAccountHistoryRepository CreateAccountHistoryRepository(IReloadingManager<string> connString, ILog log)
            {
                return new MarginTradingAccountHistoryRepository(AzureTableStorage<MarginTradingAccountHistoryEntity>.Create(connString,
                    "AccountsHistory", log));
            }

            public static MarginTradingAccountsRepository CreateAccountsRepository(IReloadingManager<string> connString, ILog log)
            {
                return new MarginTradingAccountsRepository(AzureTableStorage<MarginTradingAccountEntity>.Create(connString,
                    "MarginTradingAccounts", log));
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

            public static MatchingEngineRoutesRepository CreateMatchingEngineRoutesRepository(IReloadingManager<string> connString, ILog log)
            {
                return new MatchingEngineRoutesRepository(AzureTableStorage<MatchingEngineRouteEntity>.Create(connString,
                    "MatchingEngineRoutes", log));
            }

            public static RiskSystemCommandsLogRepository CreateRiskSystemCommandsLogRepository(IReloadingManager<string> connString, ILog log)
            {
                return new RiskSystemCommandsLogRepository(AzureTableStorage<RiskSystemCommandsLogEntity>.Create(connString,
                    "RiskSystemCommandsLog", log));
            }

            public static OvernightSwapStateRepository CreateOvernightSwapStateRepository(IReloadingManager<string> connString, ILog log)
            {
                return new OvernightSwapStateRepository(AzureTableStorage<OvernightSwapStateEntity>.Create(connString,
                    "OvernightSwapState", log));
            }

            public static OvernightSwapHistoryRepository CreateOvernightSwapHistoryRepository(IReloadingManager<string> connString, ILog log)
            {
                return new OvernightSwapHistoryRepository(AzureTableStorage<OvernightSwapHistoryEntity>.Create(connString,
                    "OvernightSwapHistory", log));
            }

            public static IDayOffSettingsRepository CreateDayOffSettingsRepository(IReloadingManager<string> connString)
            {
                return new DayOffSettingsRepository(new MarginTradingBlobRepository(connString));
            }
            
            public static IAssetPairsRepository CreateAssetPairSettingsRepository(IReloadingManager<string> connString, 
                ILog log, IConvertService convertService)
            {
                return new AssetPairsRepository(
                    AzureTableStorage<AssetPairsRepository.AssetPairEntity>.Create(connString,
                        "AssetPairs", log), convertService);
            }
        }
    }
}