using System;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IOutdatedOrderbooksService
    {
        bool IsOutdated(ExternalOrderbook orderbook, DateTime now);
    }
}
