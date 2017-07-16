﻿using AzureStorage.Queue;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common.Log;
using MarginTrading.AzureRepositories.Clients;
using MarginTrading.AzureRepositories.Monitoring;
using MarginTrading.AzureRepositories.Settings;

namespace MarginTrading.AzureRepositories
{
	public class AzureRepoFactories
	{
		public static class Clients
		{
			public static ClientSettingsRepository CreateTraderSettingsRepository(string connString, ILog log)
			{
				return new ClientSettingsRepository(new AzureTableStorage<ClientSettingsEntity>(connString, "TraderSettings", log));
			}

            public static ClientsRepository CreateClientsRepository(string connString, ILog log)
			{
			    const string tableName = "Traders";
			    return new ClientsRepository(
			        new AzureTableStorage<ClientAccountEntity>(connString, tableName, log),
			        new AzureTableStorage<AzureIndex>(connString, tableName, log));
            }
		}

		public static class Monitoring
		{
			public static ServiceMonitoringRepository CreateServiceMonitoringRepository(string connstring, ILog log)
			{
				return new ServiceMonitoringRepository(new AzureTableStorage<MonitoringRecordEntity>(connstring, "Monitoring", log));
			}
		}

		public static class Settings
		{
			public static AppGlobalSettingsRepository CreateAppGlobalSettingsRepository(string connstring, ILog log)
			{
				return new AppGlobalSettingsRepository(new AzureTableStorage<AppGlobalSettingsEntity>(connstring, "Setup", log));
			}
		}

		public static class MarginTrading
		{
			public static MarginTradingConditionsRepository CreateTradingConditionsRepository(string connstring, ILog log)
			{
				return new MarginTradingConditionsRepository(new AzureTableStorage<MarginTradingConditionEntity>(connstring,
					"MarginTradingConditions", log));
			}

			public static MarginTradingAccountGroupRepository CreateAccountGroupRepository(string connstring, ILog log)
			{
				return new MarginTradingAccountGroupRepository(new AzureTableStorage<MarginTradingAccountGroupEntity>(connstring,
					"MarginTradingAccountGroups", log));
			}

			public static MarginTradingAccountAssetsRepository CreateAccountAssetsRepository(string connstring, ILog log)
			{
				return new MarginTradingAccountAssetsRepository(new AzureTableStorage<MarginTradingAccountAssetEntity>(connstring,
					"MarginTradingAccountAssets", log));
			}

			public static MarginTradingAssetsRepository CreateAssetsRepository(string connstring, ILog log)
			{
				return new MarginTradingAssetsRepository(new AzureTableStorage<MarginTradingAssetEntity>(connstring,
					"MarginTradingAssets", log));
			}

			public static MarginTradingOrdersHistoryRepository CreateOrdersHistoryRepository(string connstring, ILog log)
			{
				return new MarginTradingOrdersHistoryRepository(new AzureTableStorage<MarginTradingOrderHistoryEntity>(connstring,
					"MarginTradingOrdersHistory", log));
			}

			public static MarginTradingOrdersRejectedRepository CreateOrdersRejectedRepository(string connstring, ILog log)
			{
				return new MarginTradingOrdersRejectedRepository(new AzureTableStorage<MarginTradingOrderRejectedEntity>(connstring,
					"MarginTradingOrdersRejected", log));
			}

			public static MarginTradingAccountHistoryRepository CreateAccountHistoryRepository(string connstring, ILog log)
			{
				return new MarginTradingAccountHistoryRepository(new AzureTableStorage<MarginTradingAccountHistoryEntity>(connstring,
					"MarginTradingAccountsHistory", log));
			}

			public static MarginTradingAccountsRepository CreateAccountsRepository(string connstring, ILog log)
			{
				return new MarginTradingAccountsRepository(new AzureTableStorage<MarginTradingAccountEntity>(connstring,
					"MarginTradingAccounts", log));
			}

			public static MarginTradingBlobRepository CreateBlobRepository(string connstring)
			{
				return new MarginTradingBlobRepository(connstring);
			}

			public static MarginTradingWatchListsRepository CreateWatchListsRepository(string connstring, ILog log)
			{
				return new MarginTradingWatchListsRepository(new AzureTableStorage<MarginTradingWatchListEntity>(connstring,
					"MarginTradingWatchLists", log));
			}

			public static MatchingEngineRoutesRepository CreateMatchingEngineRoutesRepository(string connstring, ILog log)
			{
				return new MatchingEngineRoutesRepository(new AzureTableStorage<MatchingEngineRouteEntity>(connstring,
					"MatchingEngineRoutes", log));
			}
		}
	}
}