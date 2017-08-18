using System.Threading.Tasks;
using MarginTrading.MarketMaker.Messages;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IMarketMakerService
    {
        Task ProcessNewIcmBestBidAskAsync(BestBidAskMessage bestBidAsk);
        Task ProcessNewSpotOrderBookDataAsync(OrderBookMessage orderBookMessage);
        Task ProcessAssetPairSettingsAsync(AssetPairSettingsModel message);
    }
}