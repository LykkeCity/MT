using System;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IRepeatedProblemsOrderbooksService
    {
        bool IsRepeatedProblemsOrderbook(ExternalOrderbook orderbook, bool isOutdated, bool isOutlier, DateTime now);
    }
}
