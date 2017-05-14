using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
	public class MarginTradingPositionEntity : TableEntity, IPosition
	{
		public string ClientId { get; set; }
		public string Asset { get; set; }
		public double Volume { get; set; }

		public static string GeneratePartitionKey(string asset)
		{
			return asset;
		}

		public static string GenerateRowKey(string clientId, string asset)
		{
			return $"{clientId}_{asset}";
		}

		public static MarginTradingPositionEntity Create(IPosition position)
		{
			return new MarginTradingPositionEntity
			{
				PartitionKey = GeneratePartitionKey(position.Asset),
				RowKey = GenerateRowKey(position.ClientId, position.Asset),
				Asset = position.Asset,
				ClientId = position.ClientId,
				Volume = position.Volume
			};
		}
	}
}
