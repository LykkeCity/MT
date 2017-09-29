using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Core.Helpers;
using Rocks.Caching;

namespace MarginTrading.DataReader.Services.Implementation
{
    internal class OrderBookSnapshotReaderService : IOrderBookSnapshotReaderService
    {
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly ICacheProvider _cacheProvider;
        private static string BlobName = "orderbook";

        public OrderBookSnapshotReaderService(IMarginTradingBlobRepository blobRepository, ICacheProvider cacheProvider)
        {
            _blobRepository = blobRepository;
            _cacheProvider = cacheProvider;
        }

        public async Task<OrderListPair> GetAllLimitOrders(string instrument)
        {
            var orderbookState = await GetOrderBookStateAsync();
            var orderBook = orderbookState.GetValueOrDefault(instrument, k => new OrderBook());
            return new OrderListPair
            {
                Buy = orderBook.Buy.Values.SelectMany(x => x).ToArray(),
                Sell = orderBook.Sell.Values.SelectMany(x => x).ToArray(),
            };
        }

        private Task<Dictionary<string, OrderBook>> GetOrderBookStateAsync()
        {
            return _cacheProvider.GetAsync(nameof(OrderBookSnapshotReaderService),
                async () =>
                {
                    var orderbookState = await _blobRepository.ReadAsync<Dictionary<string, OrderBook>>(
                                             LykkeConstants.StateBlobContainer, BlobName) ??
                                         new Dictionary<string, OrderBook>();
                    return new CachableResult<Dictionary<string, OrderBook>>(orderbookState,
                        CachingParameters.FromSeconds(10));
                });
        }
    }
}