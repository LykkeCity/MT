using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class IndividualValueAtRiskEntity : TableEntity
    {
		public string CounterPartyId { get; set; }

		public string AssetId { get; set; }

		public double Value { get; set; }

		private static string GetPartitionKey(string counterPartyId)
		{
			return counterPartyId;
		}

		private static string GetRowKey(string assetId)
		{
			return assetId;
		}

		public static IndividualValueAtRiskEntity Create(string counterPartyId, string assetId, double value)
		{
			return new IndividualValueAtRiskEntity
			{
				PartitionKey = GetPartitionKey(counterPartyId),
				RowKey = GetRowKey(assetId),
				CounterPartyId = counterPartyId,
				AssetId = assetId,
				Value = value
			};
		}
    }
}
