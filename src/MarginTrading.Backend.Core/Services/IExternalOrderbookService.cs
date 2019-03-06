using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orderbooks;

namespace MarginTrading.Backend.Core.Services
{
    public interface IExternalOrderbookService
    {
        Task InitializeAsync();

        void SetOrderbook(ExternalOrderBook orderbook);

        List<(string source, decimal? price)> GetOrderedPricesForExecution(string assetPairId, decimal volume,
            bool validateOppositeDirectionVolume);

        decimal? GetPriceForPositionClose(string assetPairId, decimal volume, string externalProviderId);
    }
}