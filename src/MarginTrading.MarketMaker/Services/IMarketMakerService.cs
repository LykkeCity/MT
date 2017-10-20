using System.Threading.Tasks;
using MarginTrading.MarketMaker.Messages;
using MarginTrading.MarketMaker.Models;
using MarginTrading.MarketMaker.Models.Api;

namespace MarginTrading.MarketMaker.Services
{
    public interface IMarketMakerService
    {
        Task ProcessNewExternalOrderbookAsync(ExternalExchangeOrderbookMessage orderbook);
        Task ProcessNewSpotOrderBookDataAsync(SpotOrderbookMessage orderbook);
        Task ProcessAssetPairSettingsAsync(AssetPairSettingsModel model);
    }
}