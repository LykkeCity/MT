// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineBase
    {
        string Id { get; }
        
        MatchingEngineMode Mode { get; }

        Task<MatchedOrderCollection> MatchOrderAsync(Order order, bool shouldOpenNewPosition, 
            OrderModality modality = OrderModality.Regular);
        
        (string externalProviderId, decimal? price) GetBestPriceForOpen(string assetPairId, decimal volume);
        
        decimal? GetPriceForClose(string assetPairId, decimal volume, string externalProviderId);
        
        OrderBook GetOrderBook(string instrument);
    }
}
