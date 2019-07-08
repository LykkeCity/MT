// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMarketMakerMatchingEngine : IMatchingEngineBase
    {
        void SetOrders(SetOrderModel model);
        bool PingLock();
    }
}