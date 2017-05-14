using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MarginTrading.AzureRepositories
{
	public class QuoteEntity : TableEntity
	{
		public string Instrument { get; set; }

		public string Direction { get; set; }

		public int Index { get; set; }

		public double Price { get; set; }

		public int MaxCount { get; set; }

		public long SampleInterval { get; set; }

		public static string GetPartitionKey(IQuote quote)
		{
			return $"{quote.Instrument}_{quote.Direction}";
		}

		public static string GetRowKey(int index)
		{
			return index.ToString();
		}

		public static QuoteEntity Create(IQuote quote, int index)
		{
			return new QuoteEntity
			{
				PartitionKey = GetPartitionKey(quote),
				RowKey = GetRowKey(index),
				Instrument = quote.Instrument,
				Direction = quote.Direction.ToString(),
				Index = index,
				Price = quote.Price
			};
		}

		public static string GetPartitionKey(string instrument, OrderDirection direction)
		{
			return $"{instrument}_{direction}";
		}

		public static IQuote Restore(QuoteEntity entity)
		{
			Quote quote = new Quote();

			quote.Instrument = entity.Instrument;
			quote.Price = entity.Price;

			OrderDirection direction;
			if (Enum.TryParse(entity.Direction, out direction))
			{
				quote.Direction = direction;
			}

			return quote;
		}
	}
}