using System.Collections.Generic;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Services.Events;

namespace MarginTrading.Backend.Services.Stp
{
    public class ExternalOrderBooksList
    {
        private readonly IEventChannel<BestPriceChangeEventArgs> _bestPriceChangeEventChannel;

        public ExternalOrderBooksList(IEventChannel<BestPriceChangeEventArgs> bestPriceChangeEventChannel)
        {
            _bestPriceChangeEventChannel = bestPriceChangeEventChannel;
        }
        
        //external orderbooks cache <AssetPairId, <Source, Orderbook>>
        //we assume that AssetPairId is unique in LegalEntity + STP mode
        private Dictionary<string, Dictionary<string, ExternalOrderBook>> _orderbooks;

        public List<(string source, decimal price)> GetPricesForMatch(IOrder order)
        {
            //TODO: return list of external sources with matched prices
            return null;
        }
        
        public void SetOrderbook(ExternalOrderBook orderBook)
        {
            //TODO: correctly save in cache

            //example:
            _orderbooks[orderBook.AssetPairId][orderBook.ExchangeName] = orderBook;
            
            // + calculate and produce best price (?)
            //_bestPriceChangeEventChannel.SendEvent(this, new BestPriceChangeEventArgs(null));
        }
    }
}