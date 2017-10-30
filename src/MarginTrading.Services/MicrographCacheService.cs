using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core;
using MarginTrading.Services.Events;

namespace MarginTrading.Services
{
    public sealed class MicrographCacheService : IMicrographCacheService,
        IEventConsumer<BestPriceChangeEventArgs>
    {
        private Dictionary<string, List<GraphBidAskPair>> _graphQueue;
        private readonly Dictionary<string, BidAskPair> _lastPrices;
        private const int GraphPointsCount = 150;
        private static readonly object GraphQueueLock = new object();

        public MicrographCacheService()
        {
            _lastPrices = new Dictionary<string, BidAskPair>();
            _graphQueue = new Dictionary<string, List<GraphBidAskPair>>();
        }

        public Dictionary<string, List<GraphBidAskPair>> GetGraphData()
        {
            lock (GraphQueueLock)
            {
                var copy = new Dictionary<string, List<GraphBidAskPair>>();

                foreach (var pair in _graphQueue)
                {
                    copy.Add(pair.Key, new List<GraphBidAskPair>());

                    foreach (var bidAsk in pair.Value)
                    {
                        copy[pair.Key].Add(new GraphBidAskPair
                        {
                            Ask = bidAsk.Ask,
                            Bid = bidAsk.Bid,
                            Date = bidAsk.Date
                        });
                    }
                }

                return copy;
            }
        }


        internal void InitCache(Dictionary<string, List<GraphBidAskPair>> graphData)
        {
            lock (GraphQueueLock)
            {
                _graphQueue = graphData;
            }
        }

        int IEventConsumer.ConsumerRank => 100;
        void IEventConsumer<BestPriceChangeEventArgs>.ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            var bidAskPair = ea.BidAskPair;

            if (!_lastPrices.ContainsKey(bidAskPair.Instrument))
            {
                _lastPrices.Add(bidAskPair.Instrument, bidAskPair);
            }
            else
            {
                _lastPrices[bidAskPair.Instrument] = bidAskPair;
            }

            lock (GraphQueueLock)
            {
                if (!_graphQueue.ContainsKey(bidAskPair.Instrument))
                {
                    _graphQueue.Add(bidAskPair.Instrument, new List<GraphBidAskPair>());
                }

                _graphQueue[bidAskPair.Instrument].Add(new GraphBidAskPair
                {
                    Bid = bidAskPair.Bid,
                    Ask = bidAskPair.Ask,
                    Date = DateTime.UtcNow
                });

                if (_graphQueue[bidAskPair.Instrument].Count > GraphPointsCount)
                {
                    _graphQueue[bidAskPair.Instrument] = _graphQueue[bidAskPair.Instrument]
                        .GetRange(1, _graphQueue[bidAskPair.Instrument].Count - 1);
                }
            }
        }
    }
}
