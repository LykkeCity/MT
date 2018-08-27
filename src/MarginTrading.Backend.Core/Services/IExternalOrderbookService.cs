using System.Collections.Generic;
using MarginTrading.Backend.Core.Orderbooks;

namespace MarginTrading.Backend.Core.Services
{
    public interface IExternalOrderbookService
    {
        void SetOrderbook(ExternalOrderBook orderbook);

        List<(string source, decimal? price)> GetPricesForExecution(string assetPairId, decimal volume,
            bool validateOppositeDirectionVolume);

        decimal? GetPriceForPositionClose(string assetPairId, decimal volume, string externalProviderId);
    }
}