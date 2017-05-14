using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
	public class AggregateValueAtRiskEntity : TableEntity
	{
		public string CounterPartyId { get; set; }

		public double Value { get; set; }

		private static string GetPartitionKey()
		{
			return "PVaR";
		}

		private static string GetRowKey(string counterPartyId)
		{
			return counterPartyId;
		}

		public static AggregateValueAtRiskEntity Create(string counterPartyId, double value)
		{
			return new AggregateValueAtRiskEntity
			{
				PartitionKey = GetPartitionKey(),
				RowKey = GetRowKey(counterPartyId),
				CounterPartyId = counterPartyId,
				Value = value
			};
		}
	}
}
