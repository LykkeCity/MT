// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;

namespace MarginTrading.Backend.Services
{
    public class OrderBookSaveService : TimerPeriod
    {
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly OrderBookList _orderBookList;
        private readonly ILog _log;

        private static string BlobName = "orderbook";
        private readonly IContextFactory _contextFactory;

        public OrderBookSaveService(
            IMarginTradingBlobRepository blobRepository,
            OrderBookList orderBookList,
            MarginTradingSettings marginTradingSettings,
            ILog log,
            IContextFactory contextFactory) 
            : base(nameof(OrderBookSaveService), marginTradingSettings.BlobPersistence.OrderbooksDumpPeriodMilliseconds, log)
        {
            _blobRepository = blobRepository;
            _orderBookList = orderBookList;
            _log = log;
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
            if (Working)
            {
                DumpToRepository().Wait();
            }

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
                    await _blobRepository.WriteAsync(LykkeConstants.StateBlobContainer, BlobName, orderbookState);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(OrderBookSaveService), "Save orderbook", "", ex);
            }
        }
    }
}
