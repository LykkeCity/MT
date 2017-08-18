using System;

namespace MarginTrading.MarketMaker.HelperServices
{
    internal interface ISystem
    {
        DateTime Now { get; }
    }
}