using System.Collections.Generic;
using MarginTrading.MarketMaker.Messages;

namespace MarginTrading.MarketMaker.Services
{
    /// <summary>
    /// Generates order commands from spot orderbooks.
    /// </summary>
    /// <remarks>
    /// From spot we receive only half of orderbook in a single message - either buy or sell.
    /// If bba from new message leads to a negative spread -
    /// this price is added to pending and then is returned with next complementing message,
    /// meaning a positive spread.
    /// </remarks>
    internal interface ISpotOrderCommandsGeneratorService
    {
        IReadOnlyList<OrderCommand> GenerateOrderCommands(string assetPairId, bool isBuy, decimal newBestPrice, decimal ordersVolume);
    }
}