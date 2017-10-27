using System;

namespace MarginTrading.MarketMaker.HelperServices
{
    public interface ISystem
    {
        DateTime UtcNow { get; }
    }
}