using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
	public class StDevEntity : TableEntity
	{
		public string Asset { get; set; }
		public double Value { get; set; }

		private static string GetPartitionKey()
		{
			return "StDev";
		}

		private static string GetRowKey(string asset)
		{
			return asset;
		}

		public static StDevEntity Create(string asset, double value)
		{
			return new StDevEntity
			{
				PartitionKey = GetPartitionKey(),
				RowKey = GetRowKey(asset),
				Asset = asset,
				Value = value
			};
		}
	}

    public class MarginTradingStDevVectorRepository : IMarginTradingStDevVectorRepository
	{
		private readonly INoSQLTableStorage<StDevEntity> _tableStorage;

		public MarginTradingStDevVectorRepository(INoSQLTableStorage<StDevEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}

		public async Task Save(Dictionary<string, double> stDevVector)
		{
			foreach (KeyValuePair<string, double> kvp in stDevVector)
			{
				await _tableStorage.InsertOrReplaceAsync(StDevEntity.Create(kvp.Key, kvp.Value));
			}
		}
	}
}
