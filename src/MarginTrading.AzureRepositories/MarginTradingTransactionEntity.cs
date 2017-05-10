using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingTransactionEntity : TableEntity
    {
        public string TakerOrderId { get; set; }
        public string TakerLykkeId { get; set; }
        public string TakerAccountId { get; set; }
        public double? TakerSpread { get; set; }

        public string MakerOrderId { get; set; }
        public string MakerLykkeId { get; set; }
        public double? MakerSpread { get; set; }

        public string LykkeExecutionId { get; set; }
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
        public string OrderId { get; set; }

        public static string GeneratePartitionKey(string makerId, string symbol)
        {
            return $"{makerId}_{symbol}";
        }

        public static string GenerateRowKey(string traderOrderId, string makerOrderId, string action)
        {
            return $"{traderOrderId}_{makerOrderId}_{action}";
        }

        public static MarginTradingTransactionEntity Create(ITransaction transaction)
        {
            return new MarginTradingTransactionEntity
            {
                PartitionKey = GeneratePartitionKey(transaction.MakerLykkeId, transaction.CoreSymbol),
                RowKey = GenerateRowKey(transaction.TakerOrderId, transaction.MakerOrderId, transaction.TakerAction.ToString()),
                TakerOrderId = transaction.TakerOrderId,
                TakerLykkeId = transaction.TakerLykkeId,
                TakerAccountId = transaction.TakerAccountId,
                TakerSpread = transaction.TakerSpread,

                MakerOrderId = transaction.MakerOrderId,
                MakerLykkeId = transaction.MakerLykkeId,
                MakerSpread = transaction.MakerSpread,

                LykkeExecutionId = transaction.LykkeExecutionId,
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
                OrderId = transaction.OrderId
            };
        }

        public static ITransaction Restore(MarginTradingTransactionEntity entity)
        {
            var transaction = new Transaction();

            transaction.TakerOrderId = entity.TakerOrderId;
            transaction.TakerLykkeId = entity.TakerLykkeId;
            transaction.TakerAccountId = entity.TakerAccountId;
            transaction.TakerSpread = entity.TakerSpread;

            transaction.MakerOrderId = entity.MakerOrderId;
            transaction.MakerLykkeId = entity.MakerLykkeId;
            transaction.MakerSpread = entity.MakerSpread;

            transaction.LykkeExecutionId = entity.LykkeExecutionId;

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
            transaction.OrderId = entity.OrderId;

            return transaction;
        }
    }
}
