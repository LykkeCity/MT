using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MarginTrading.AzureRepositories
{
	public class ElementaryTransactionEntity : TableEntity
	{
		public string CounterPartyId { get; set; }
		public string AccountId { get; set; }
		public string Asset { get; set; }
		public double? Amount { get; set; }
		public string TradingTransactionId { get; set; }
		public string TradingOrderId { get; set; }
		public string PositionId { get; set; }
		public double? AmountInUsd { get; set; }
		public DateTime? TimeStamp { get; set; }
		public string Type { get; set; }
		public string CoreSymbol { get; set; }
		public string AssetSide { get; set; }
		public string SubType { get; set; }

		public static string GeneratePartitionKey(IElementaryTransaction transaction)
		{
			return $"{transaction.CounterPartyId}_{transaction.Asset}";
		}

		public static string GenerateRowKey(IElementaryTransaction transaction)
		{
			return $"{transaction.TradingTransactionId}_{transaction.Asset}_{transaction.SubType}_{transaction.CounterPartyId}";
		}

		public static IElementaryTransaction Restore(ElementaryTransactionEntity entity)
		{
			if (entity == null)
				return null;

			var transaction = new ElementaryTransaction();

			transaction.CounterPartyId = entity.CounterPartyId;
			transaction.AccountId = entity.AccountId;
			transaction.Asset = entity.Asset;
			transaction.CoreSymbol = entity.CoreSymbol;
			transaction.Amount = entity.Amount;
			transaction.TradingTransactionId = entity.TradingTransactionId;
			transaction.TradingOrderId = entity.TradingOrderId;
			transaction.PositionId = entity.PositionId;
			transaction.AmountInUsd = entity.AmountInUsd;
			transaction.TimeStamp = entity.TimeStamp;
			transaction.SubType = entity.SubType;

			ElementaryTransactionType type;

			if (Enum.TryParse(entity.Type, out type))
			{
				transaction.Type = type;
			}

			AssetSide assetSide;

			if (Enum.TryParse(entity.AssetSide, out assetSide))
			{
				transaction.AssetSide = assetSide;
			}

			return transaction;
		}

		public static ElementaryTransactionEntity Create(IElementaryTransaction transaction)
		{
			return new ElementaryTransactionEntity
			{
				PartitionKey = GeneratePartitionKey(transaction),
				RowKey = GenerateRowKey(transaction),
				CounterPartyId = transaction.CounterPartyId,
				AccountId = transaction.AccountId,
				Asset = transaction.Asset,
				CoreSymbol = transaction.CoreSymbol,
				AssetSide = transaction.AssetSide.ToString(),
				Amount = transaction.Amount,
				TradingTransactionId = transaction.TradingTransactionId,
				AmountInUsd = transaction.AmountInUsd,
				TimeStamp = transaction.TimeStamp,
				Type = transaction.Type.ToString(),
				TradingOrderId = transaction.TradingOrderId,
				PositionId = transaction.PositionId,
				SubType = transaction.SubType
			};
		}
	}
}