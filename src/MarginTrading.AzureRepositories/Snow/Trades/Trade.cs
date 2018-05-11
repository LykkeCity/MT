using System;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.AzureRepositories.Snow.Trades
{
    public class Trade : ITrade
    {
        public Trade(string id, string orderId, string positionId, string accountId, DateTime tradeTimestamp,
            string assetPairId, TradeType type, decimal price, decimal volume)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            OrderId = orderId ?? throw new ArgumentNullException(nameof(orderId));
            PositionId = positionId ?? throw new ArgumentNullException(nameof(positionId));
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            TradeTimestamp = tradeTimestamp;
            AssetPairId = assetPairId ?? throw new ArgumentNullException(nameof(assetPairId));
            Type = type;
            Price = price;
            Volume = volume;
        }

        public string Id { get; }
        public string OrderId { get; }
        public string PositionId { get; }
        public string AccountId { get; }
        public DateTime TradeTimestamp { get; }
        public string AssetPairId { get; }
        public TradeType Type { get; }
        public decimal Price { get; }
        public decimal Volume { get; }
    }
}