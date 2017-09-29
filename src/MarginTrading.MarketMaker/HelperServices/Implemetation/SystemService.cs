using System;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    internal class SystemService : ISystem
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
