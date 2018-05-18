using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.AzureRepositories.Snow.OrdersById;
using MarginTrading.AzureRepositories.Snow.OrdersHistory;
using MarginTrading.AzureRepositories.Snow.Trades;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services;

namespace MarginTrading.AzureRepositories
{
    public class AzureRepoFactories
    {
        public static class MarginTrading
        {
            public static OrdersHistoryRepository CreateOrdersHistoryRepository(IReloadingManager<string> connString, ILog log)
            {
                return new OrdersHistoryRepository(AzureTableStorage<OrderHistoryEntity>.Create(connString,
                    "MarginTradingOrdersHistory", log));
            }

            public static ITradesRepository CreateTradesRepository(IReloadingManager<string> connString, ILog log,
                IConvertService resolve)
            {
                return new TradesRepository(AzureTableStorage<TradeEntity>.Create(connString, "Trades", log));
            }

            public static IOrdersByIdRepository CreateOrdersByIdRepository(IReloadingManager<string> connString, ILog log,
                IConvertService resolve)
            {
                return new OrdersByIdRepository(AzureTableStorage<OrderByIdEntity>.Create(connString, "OrdersById", log));
            }

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

            public static IDayOffSettingsRepository CreateDayOffSettingsRepository(IReloadingManager<string> connString)
            {
                return new DayOffSettingsRepository(new MarginTradingBlobRepository(connString));
            }
        }
    }
}