using System;
using Lykke.AzureStorage.Tables;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.AzureRepositories.Snow.Trades
{
    internal class TradeEntity : AzureTableEntity, ITrade
    {
        public TradeEntity()
        {
            RowKey = GetRowKey();
        }

        public string Id
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public string OrderId { get; set; }
        public string PositionId { get; set; }
        public string AccountId { get; set; }
        public DateTime TradeTimestamp { get; set; }
        public string AssetPairId { get; set; }
        public TradeType Type { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }

        public static string GetPartitionKey(string id)
        {
            return id;
        }

        public static string GetRowKey()
        {
            return "-";
        }

        public static TradeEntity Create(ITrade trade)
        {
            return new TradeEntity
            {
                AccountId = trade.AccountId,
                AssetPairId = trade.AssetPairId,
                Id = trade.Id,
                OrderId = trade.OrderId,
                PositionId = trade.PositionId,
                Price = trade.Price,
                TradeTimestamp = trade.TradeTimestamp,
                Type = trade.Type,
                Volume = trade.Volume,
            };
        }
    }
}