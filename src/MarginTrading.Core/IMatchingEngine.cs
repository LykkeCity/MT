using System;
using System.Collections.Generic;

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
        public string[] DeleteByInstrumentsBuy { get; set; }
        public string[] DeleteByInstrumentsSell { get; set; }
        public LimitOrder[] OrdersToAdd { get; set; }
        public string[] OrderIdsToDelete { get; set; }
    }
}
