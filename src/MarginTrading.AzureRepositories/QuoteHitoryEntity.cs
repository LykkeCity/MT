using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MarginTrading.AzureRepositories
{
	public class QuoteHistoryEntityItem
	{
		public string A { get; set; }

		public DateTime T { get; set; }

		public double P { get; set; }
	}

	public class QuoteHistoryEntity : TableEntity
	{
		public string Part000 { get; set; }
		public QuoteHistoryEntityItem[] Items { get; set; }

		public static string GetPartitionKey(string instrument, OrderDirection direction)
		{
			return $"{instrument}_{direction.ToString().ToUpper()}";
		}

		public static string GetRowKey(long ticks)
		{
			return string.Format("{0:0000000000000000000}", ticks);
		}
	}
}
