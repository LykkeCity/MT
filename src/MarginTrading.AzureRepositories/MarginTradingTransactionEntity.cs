using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MarginTrading.AzureRepositories
{
	public class MarginTradingTransactionEntity : TableEntity
	{
		public string TakerPositionId { get; set; }
		public string TakerOrderId { get; set; }
		public string TakerCounterpartyId { get; set; }
		public string TakerAccountId { get; set; }
		public double? TakerSpread { get; set; }

		public string MakerOrderId { get; set; }
		public string MakerCounterpartyId { get; set; }
		public string MakerAccountId { get; set; }
		public double? MakerSpread { get; set; }

		public string TradingTransactionId { get; set; }
		public string CoreSide { get; set; }
		public string CoreSymbol { get; set; }
		public DateTime? ExecutedTime { get; set; }
		public double? ExecutionDuration { get; set; }
		public double FilledVolume { get; set; }
		public double Price { get; set; }
		public double? VolumeInUSD { get; set; }
		public double? ExchangeMarkup { get; set; }
		public double? CoreSpread { get; set; }
		public string Comment { get; set; }
		public string GeneratedFrom { get; set; }
		public double? TakerProfit { get; set; }

		public static string GeneratePartitionKey(string makerId, string symbol)
		{
			return $"{makerId}_{symbol}";
		}

		public static string GenerateRowKey(string transactionId)
		{
			return transactionId;
		}

		public static MarginTradingTransactionEntity Create(ITransaction transaction)
		{
			return new MarginTradingTransactionEntity
			{
				PartitionKey = GeneratePartitionKey(transaction.MakerCounterpartyId, transaction.CoreSymbol),
				RowKey = GenerateRowKey(transaction.TradingTransactionId),
				TakerPositionId = transaction.TakerPositionId,
				TakerOrderId = transaction.TakerOrderId,
				TakerCounterpartyId = transaction.TakerCounterpartyId,
				TakerAccountId = transaction.TakerAccountId,
				TakerSpread = transaction.TakerSpread,

				MakerOrderId = transaction.MakerOrderId,
				MakerCounterpartyId = transaction.MakerCounterpartyId,
				MakerAccountId = transaction.MakerAccountId,
				MakerSpread = transaction.MakerSpread,

				TradingTransactionId = transaction.TradingTransactionId,
				CoreSide = transaction.CoreSide.ToString(),
				CoreSymbol = transaction.CoreSymbol,
				ExecutedTime = transaction.ExecutedTime,
				ExecutionDuration = transaction.ExecutionDuration,
				FilledVolume = transaction.FilledVolume,
				Price = transaction.Price,
				VolumeInUSD = transaction.VolumeInUSD,
				ExchangeMarkup = transaction.ExchangeMarkup,
				CoreSpread = transaction.CoreSpread,
				Comment = transaction.Comment,
				GeneratedFrom = transaction.IsLive ? "Live" : "History",
				TakerProfit = transaction.TakerProfit
			};
		}

		public static ITransaction Restore(MarginTradingTransactionEntity entity)
		{
			var transaction = new Transaction();

			transaction.TakerPositionId = entity.TakerPositionId;
			transaction.TakerOrderId = entity.TakerOrderId;
			transaction.TakerCounterpartyId = entity.TakerCounterpartyId;
			transaction.TakerAccountId = entity.TakerAccountId;
			transaction.TakerSpread = entity.TakerSpread;

			transaction.MakerOrderId = entity.MakerOrderId;
			transaction.MakerCounterpartyId = entity.MakerCounterpartyId;
			transaction.MakerAccountId = entity.MakerAccountId;
			transaction.MakerSpread = entity.MakerSpread;

			transaction.TradingTransactionId = entity.TradingTransactionId;

			OrderDirection coreSide;

			if (Enum.TryParse(entity.CoreSide, out coreSide))
			{
				transaction.CoreSide = coreSide;
			}

			transaction.CoreSymbol = entity.CoreSymbol;
			transaction.ExecutedTime = entity.ExecutedTime;
			transaction.ExecutionDuration = entity.ExecutionDuration;
			transaction.FilledVolume = entity.FilledVolume;
			transaction.Price = entity.Price;
			transaction.VolumeInUSD = entity.VolumeInUSD;
			transaction.ExchangeMarkup = entity.ExchangeMarkup;
			transaction.CoreSpread = entity.CoreSpread;
			transaction.Comment = entity.Comment;
			transaction.TakerProfit = entity.TakerProfit;

			return transaction;
		}
	}
}