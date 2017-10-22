using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IBestPricesService
    {
        BestPrices Calc(Orderbook orderbook);
    }
}