using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
	public class PearsonCoeffEntity : TableEntity
	{
		public string Asset_x { get; set; }
		public string Asset_y { get; set; }
		public double Value { get; set; }

		private static string GetPartitionKey(string asset_x)
		{
			return asset_x;
		}

		private static string GetRowKey(string asset_y)
		{
			return asset_y;
		}

		public static PearsonCoeffEntity Create(string asset_x, string asset_y, double value)
		{
			return new PearsonCoeffEntity
			{
				PartitionKey = GetPartitionKey(asset_x),
				RowKey = GetRowKey(asset_y),
				Asset_x = asset_x,
				Asset_y = asset_y,
				Value = value
			};
		}
	}

    public class MarginTradingPearsonCoeffMatrixRepository : IMarginTradingPearsonCorrMatrixRepository
	{
		private readonly INoSQLTableStorage<PearsonCoeffEntity> _tableStorage;

		public MarginTradingPearsonCoeffMatrixRepository(INoSQLTableStorage<PearsonCoeffEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}

		public async Task Save(Dictionary<string, Dictionary<string, double>> matrix)
		{
			foreach (KeyValuePair<string, Dictionary<string, double>> kvp_x in matrix)
			{
				foreach (KeyValuePair<string, double> kvp_y in kvp_x.Value)
				{
					await _tableStorage.InsertOrReplaceAsync(PearsonCoeffEntity.Create(kvp_x.Key, kvp_y.Key, kvp_y.Value));
				}
			}
		}
	}
}
