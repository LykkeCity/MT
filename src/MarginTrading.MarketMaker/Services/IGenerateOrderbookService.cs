using JetBrains.Annotations;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IGenerateOrderbookService
    {
        [CanBeNull]
        Orderbook OnNewOrderbook(ExternalOrderbook orderbook);
    }
}