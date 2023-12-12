// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Services
{
    public interface ISentimentCache
    {
        void Subscribe(IObservable<Position> provider);

        void Initialize(IReadOnlyCollection<Position> positions);
        
        (decimal, decimal) Get(string productId);

        IReadOnlyCollection<Sentiment> GetAll();
        
        IReadOnlyCollection<Sentiment> GetFiltered(HashSet<string> productIds);
    }
}