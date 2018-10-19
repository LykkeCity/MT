using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class MarketMakerMatchingEngine : IMarketMakerMatchingEngine
    {
        private readonly IEventChannel<BestPriceChangeEventArgs> _bestPriceChangeEventChannel;
        private readonly OrderBookList _orderBooks;
        private readonly IContextFactory _contextFactory;

        public string Id { get; }

        public MatchingEngineMode Mode => MatchingEngineMode.MarketMaker;

        public MarketMakerMatchingEngine(
            string id,
            IEventChannel<BestPriceChangeEventArgs> bestPriceChangeEventChannel,
            OrderBookList orderBooks, 
            IContextFactory contextFactory)
        {
            Id = id;
            _bestPriceChangeEventChannel = bestPriceChangeEventChannel;
            _orderBooks = orderBooks;
            _contextFactory = contextFactory;
        }

        public void SetOrders(SetOrderModel model)
        {
            var updatedInstruments = new List<string>();
            
            using (_contextFactory.GetWriteSyncContext($"{nameof(MarketMakerMatchingEngine)}.{nameof(SetOrders)}"))
            {
                if (model.DeleteByInstrumentsBuy?.Count > 0)
                {
                    _orderBooks.DeleteAllBuyOrdersByMarketMaker(model.MarketMakerId, model.DeleteByInstrumentsBuy);
                }

                if (model.DeleteByInstrumentsSell?.Count > 0)
                {
                    _orderBooks.DeleteAllSellOrdersByMarketMaker(model.MarketMakerId, model.DeleteByInstrumentsSell);
                }

                if (model.DeleteAllBuy || model.DeleteAllSell)
                {
                    _orderBooks.DeleteAllOrdersByMarketMaker(model.MarketMakerId, model.DeleteAllBuy,
                        model.DeleteAllSell);
                }

                if (model.OrderIdsToDelete?.Length > 0)
                {
                    var deletedOrders =
                        _orderBooks.DeleteMarketMakerOrders(model.MarketMakerId, model.OrderIdsToDelete);

                    updatedInstruments.AddRange(deletedOrders.Select(o => o.Instrument).Distinct());
                }

                if (model.OrdersToAdd?.Count > 0)
                {
                    _orderBooks.AddMarketMakerOrders(model.OrdersToAdd);

                    updatedInstruments.AddRange(model.OrdersToAdd.Select(o => o.Instrument).Distinct());
                }

                foreach (var instrument in updatedInstruments.Distinct())
                {
                    ProduceBestPrice(instrument);
                }
            }
        }

        public decimal? GetPriceForClose(string assetPairId, decimal volume, string externalProviderId)
        {
            using (_contextFactory.GetWriteSyncContext($"{nameof(MarketMakerMatchingEngine)}.{nameof(GetPriceForClose)}"))
            {
                var orderBookTypeToMatch =
                    volume.GetClosePositionOrderDirection().GetOrderDirectionToMatchInOrderBook();

                var matchedOrders = _orderBooks.Match(assetPairId, orderBookTypeToMatch, Math.Abs(volume));

                return matchedOrders.Any() ? matchedOrders.WeightedAveragePrice : (decimal?) null;
            } // lock
        }

        public OrderBook GetOrderBook(string instrument)
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(MarketMakerMatchingEngine)}.{nameof(GetOrderBook)}"))
            {
                 return _orderBooks.GetOrderBook(instrument);
            }
        }

        public Task<MatchedOrderCollection> MatchOrderAsync(Order order, bool shouldOpenNewPosition,
            OrderModality modality = OrderModality.Regular)
        {
            using (_contextFactory.GetWriteSyncContext(
                $"{nameof(MarketMakerMatchingEngine)}.{nameof(MatchOrderAsync)}"))
            {
                var orderBookTypeToMatch = order.Direction.GetOrderDirectionToMatchInOrderBook();
                
                //TODO: validate oposite direction if will open new porition

                var matchedOrders =
                    _orderBooks.Match(order.AssetPairId, orderBookTypeToMatch, Math.Abs(order.Volume));

                _orderBooks.Update(order.AssetPairId, orderBookTypeToMatch, matchedOrders);
                ProduceBestPrice(order.AssetPairId);

                return Task.FromResult(matchedOrders);
            }
        }

        public bool PingLock()
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(MarketMakerMatchingEngine)}.{nameof(PingLock)}"))
            {
                return true;
            }
        }

        private void ProduceBestPrice(string instrument)
        {
            var bestPrice = _orderBooks.GetBestPrice(instrument);

            if (bestPrice != null)
                _bestPriceChangeEventChannel.SendEvent(this, new BestPriceChangeEventArgs(bestPrice));
        }
    }
}
