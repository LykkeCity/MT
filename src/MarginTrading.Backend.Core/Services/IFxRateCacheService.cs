// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.OrderbookAggregator.Contracts.Messages;

namespace MarginTrading.Backend.Core.Services
{
    public interface IFxRateCacheService
    {
        InstrumentBidAskPair GetQuote(string instrument);
        Dictionary<string, InstrumentBidAskPair> GetAllQuotes();
        Task SetQuote(ExternalExchangeOrderbookMessage orderBookMessage);
        void SetQuote(InstrumentBidAskPair bidAskPair);
        void RemoveQuote(string assetPairId);
    }
}