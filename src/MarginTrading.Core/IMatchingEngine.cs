using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Core
{
    public interface IMatchingEngine : IMatchingEngineBase
    {
        void SetOrders(SetOrderModel model);
        void MatchMarketOrderForOpen(Order order, Func<MatchedOrder[], bool> orderProcessed);
        void MatchMarketOrderForClose(Order order, Func<MatchedOrder[], bool> orderProcessed);
        Dictionary<string, OrderBook> GetOrderBook(List<string> marketMakerIds);
        bool PingLock();
    }

    public class SetOrderModel
    {
        public string MarketMakerId { get; set; }
        public bool DeleteAllBuy { get; set; }
        public bool DeleteAllSell { get; set; }
        [CanBeNull]
        public IReadOnlyList<string> DeleteByInstrumentsBuy { get; set; }
        [CanBeNull]
        public IReadOnlyList<string> DeleteByInstrumentsSell { get; set; }
        [CanBeNull]
        public IReadOnlyList<LimitOrder> OrdersToAdd { get; set; }
        public string[] OrderIdsToDelete { get; set; }
    }
}
