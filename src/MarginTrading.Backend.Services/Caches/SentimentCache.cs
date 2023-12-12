// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MoreLinq;

namespace MarginTrading.Backend.Services.Caches
{
    public class SentimentCache : ISentimentCache, IObserver<Position>
    {
        private IDisposable _unsubscriber;
        private readonly ILog _log;

        private readonly ConcurrentDictionary<string, Sentiment> _sentiments =
            new ConcurrentDictionary<string, Sentiment>();

        public SentimentCache(ILog log)
        {
            _log = log;
        }
        
        public void Initialize(IReadOnlyCollection<Position> positions)
        {
            positions
                .GroupBy(p => p.AssetPairId)
                .ForEach(g =>
                {
                    var shortCounter = g.Count(p => p.Direction == PositionDirection.Short);
                    var longCounter = g.Count() - shortCounter;
                    var added = _sentiments.TryAdd(g.Key, new Sentiment(g.Key, shortCounter, longCounter));

                    if (!added)
                    {
                        _log.WriteWarning(nameof(Initialize), g.Key, "Failed to initialize product sentiment");
                    }
                });
        }

        public (decimal, decimal) Get(string productId)
        {
            if (_sentiments.TryGetValue(productId, out var sentiment))
            {
                var (shortShare, longShare) = sentiment;
                return (shortShare, longShare);
            }
            
            return (0, 0);
        }

        public IReadOnlyCollection<Sentiment> GetAll() => _sentiments.Values.ToList();

        public IReadOnlyCollection<Sentiment> GetFiltered(HashSet<string> productIds) => _sentiments.Values
            .Where(x => productIds.Contains(x.ProductId))
            .ToList();

        public void Subscribe(IObservable<Position> provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _unsubscriber = provider.Subscribe(this);
        }

        public void OnCompleted()
        {
            _log.WriteInfo(nameof(OnCompleted), null, "Unsubscribing from positions");
            _unsubscriber.Dispose();
        }

        public void OnError(Exception error)
        {
            _log.WriteError(nameof(OnError), null, error);
        }

        public void OnNext(Position value)
        {
            var isShort = value.Direction == PositionDirection.Short;
            var productId = value.AssetPairId;
            
            switch (value.Status)
            {
                case PositionStatus.Active:
                    _sentiments.AddOrUpdate(productId,
                        isShort ? new Sentiment(productId, shortCounter: 1) : new Sentiment(productId, longCounter: 1),
                        (_, old) => isShort ? old.AddShort() : old.AddLong());
                    break;
                case PositionStatus.Closing:
                case PositionStatus.Closed:
                    _sentiments.AddOrUpdate(productId, 
                        new Sentiment(productId), 
                        (_, old) => isShort ? old.RemoveShort() : old.RemoveLong());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value.Status),"Unexpected position status: " + value.Status);

            }
        }
    }
}