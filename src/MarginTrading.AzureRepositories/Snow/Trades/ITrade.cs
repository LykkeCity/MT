using System;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.AzureRepositories.Snow.Trades
{
    public interface ITrade
    {
        string Id { get; }
        string OrderId { get; }
        string PositionId { get; }
        string AccountId { get; }
        DateTime TradeTimestamp { get; }
        string AssetPairId { get; }
        TradeType Type { get; }
        decimal Price { get; }
        decimal Volume { get; }
    }
}