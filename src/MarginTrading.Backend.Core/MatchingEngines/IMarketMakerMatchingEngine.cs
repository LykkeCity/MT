// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMarketMakerMatchingEngine : IMatchingEngineBase
    {
        void SetOrders(SetOrderModel model);
        bool PingLock();
    }
}