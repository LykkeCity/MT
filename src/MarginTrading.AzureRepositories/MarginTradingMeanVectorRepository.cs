using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
	public class MeanEntity : TableEntity
	{
		public string Asset { get; set; }
		public double Value { get; set; }

		private static string GetPartitionKey()
		{
			return "Mean";
		}

		private static string GetRowKey(string asset)
		{
			return asset;
		}

		public static MeanEntity Create(string asset, double value)
		{
			return new MeanEntity
			{
				PartitionKey = GetPartitionKey(),
				RowKey = GetRowKey(asset),
				Asset = asset,
				Value = value
			};
		}
	}

    public class MarginTradingMeanVectorRepository : IMarginTradingMeanVectorRepository
	{
		private readonly INoSQLTableStorage<MeanEntity> _tableStorage;

		public MarginTradingMeanVectorRepository(INoSQLTableStorage<MeanEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}

		public async Task Save(Dictionary<string, double> stDevVector)
		{
			foreach (KeyValuePair<string, double> kvp in stDevVector)
			{
				await _tableStorage.InsertOrReplaceAsync(MeanEntity.Create(kvp.Key, kvp.Value));
			}
		}
	}
}
