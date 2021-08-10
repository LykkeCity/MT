// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Backend.Core.Orderbooks;

namespace MarginTrading.Backend.Core.Services
{
    public interface IExternalOrderbookService : IStartable
    {
        List<ExternalOrderBook> GetOrderBooks();

        void SetOrderbook(ExternalOrderBook orderbook);

        List<(string source, decimal? price)> GetOrderedPricesForExecution(string assetPairId, decimal volume,
            bool validateOppositeDirectionVolume);

        decimal? GetPriceForPositionClose(string assetPairId, decimal volume, string externalProviderId);
        ExternalOrderBook GetOrderBook(string assetPairId);
    }
}