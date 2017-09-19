using System.Threading.Tasks;
using MarginTrading.MarketMaker.Messages;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IMarketMakerService
    {
        Task ProcessNewExternalOrderbookAsync(ExternalExchangeOrderbookMessage orderbook);
        Task ProcessNewSpotOrderBookDataAsync(SpotOrderbookMessage spotOrderbookMessage);
        Task ProcessAssetPairSettingsAsync(AssetPairSettingsModel model);
    }
}