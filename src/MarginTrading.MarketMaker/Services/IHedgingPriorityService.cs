using System.Collections.Immutable;

namespace MarginTrading.MarketMaker.Services
{
    public interface IHedgingPriorityService
    {
        /// <summary>
        /// Returns exchanges available for hedging with their preference amount from 0 to 1.
        /// 0 means hedging is unavailable, and 1 means very good.
        /// </summary>
        ImmutableDictionary<string, decimal> Get(string assetPairId);
    }
}
