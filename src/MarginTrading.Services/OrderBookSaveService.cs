using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Core;
using MarginTradingHelpers = MarginTrading.Services.Helpers.MarginTradingHelpers;

namespace MarginTrading.Services
{
    public class OrderBookSaveService : TimerPeriod
    {
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly OrderBookList _orderBookList;
        private readonly ILog _log;
        private readonly IAccountAssetsCacheService _accountAssetsCache;

        private static string BlobName = "orderbook";

        public OrderBookSaveService(
            IMarginTradingBlobRepository blobRepository,
            OrderBookList orderBookList,
            ILog log,
            IAccountAssetsCacheService accountAssetsCache
        ) : base(nameof(OrderBookSaveService), 5000, log)
        {
            _blobRepository = blobRepository;
            _orderBookList = orderBookList;
            _log = log;
            _accountAssetsCache = accountAssetsCache;
        }

        public override void Start()
        {
            var state =
                _blobRepository.Read<Dictionary<string, OrderBook>>(LykkeConstants.StateBlobContainer, BlobName)
                    ?.ToDictionary(d => d.Key, d => d.Value) ??
                new Dictionary<string, OrderBook>();

            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                _orderBookList.Init(state);
            }

            base.Start();
        }

        public override Task Execute()
        {
            return DumpToRepository();
        }

        public override void Stop()
        {
            DumpToRepository().Wait();
            base.Stop();
        }

        private async Task DumpToRepository()
        {
            try
            {
                Dictionary<string, OrderBook> orderbookState;

                lock (MarginTradingHelpers.TradingMatchingSync)
                    orderbookState = _orderBookList.GetOrderBookState();

                if (orderbookState != null)
                {
                    await _blobRepository.Write(LykkeConstants.StateBlobContainer, BlobName, orderbookState);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(OrderBookSaveService), "Save orderbook", "", ex);
            }
        }
    }
}
