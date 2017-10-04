using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Core;
using MarginTrading.Services.Infrastructure;

namespace MarginTrading.Services
{
    public class OrderBookSaveService : TimerPeriod
    {
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly OrderBookList _orderBookList;
        private readonly ILog _log;
        private readonly IAccountAssetsCacheService _accountAssetsCache;

        private static string BlobName = "orderbook";
        private readonly IContextFactory _contextFactory;

        public OrderBookSaveService(
            IMarginTradingBlobRepository blobRepository,
            OrderBookList orderBookList,
            ILog log,
            IAccountAssetsCacheService accountAssetsCache, 
            IContextFactory contextFactory) : base(nameof(OrderBookSaveService), 5000, log)
        {
            _blobRepository = blobRepository;
            _orderBookList = orderBookList;
            _log = log;
            _accountAssetsCache = accountAssetsCache;
            _contextFactory = contextFactory;
        }

        public override void Start()
        {
            var state =
                _blobRepository.Read<Dictionary<string, OrderBook>>(LykkeConstants.StateBlobContainer, BlobName)
                    ?.ToDictionary(d => d.Key, d => d.Value) ??
                new Dictionary<string, OrderBook>();

            using (_contextFactory.GetWriteSyncContext($"{nameof(OrderBookSaveService)}.{nameof(Start)}"))
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

                using (_contextFactory.GetReadSyncContext($"{nameof(OrderBookSaveService)}.{nameof(DumpToRepository)}"))
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
