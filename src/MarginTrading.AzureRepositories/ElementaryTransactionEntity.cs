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
        public double? AmountInUsd { get; set; }
        public DateTime? TimeStamp { get; set; }

        public static string GeneratePartitionKey(IElementaryTransaction transaction)
        {
            return $"{transaction.CounterPartyId}_{transaction.Asset}";
        }

        public static string GenerateRowKey(IElementaryTransaction transaction)
        {
            return $"{transaction.TradingTransactionId}_{transaction.Asset}_{transaction.CounterPartyId}";
        }

        public static IElementaryTransaction Restore(ElementaryTransactionEntity entity)
        {
            if (entity == null)
                return null;

            var transaction = new ElementaryTransaction();

            transaction.CounterPartyId = entity.CounterPartyId;
            transaction.AccountId = entity.AccountId;
            transaction.Asset = entity.Asset;
            transaction.Amount = entity.Amount;
            transaction.TradingTransactionId = entity.TradingTransactionId;
            transaction.AmountInUsd = entity.AmountInUsd;
            transaction.TimeStamp = entity.TimeStamp;

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
                Amount = transaction.Amount,
                TradingTransactionId = transaction.TradingTransactionId,
                AmountInUsd = transaction.AmountInUsd,
                TimeStamp = transaction.TimeStamp
            };
        }
    }
}