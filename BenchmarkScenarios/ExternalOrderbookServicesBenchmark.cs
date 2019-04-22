using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Common.Log;
using Lykke.MarginTrading.OrderBookService.Contracts;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.Common.Services;
using Moq;

namespace BenchmarkScenarios
{
    [CoreJob]
    [RPlotExporter, RankColumn]
    public class ExternalOrderbookServicesBenchmark
    {
        private ExternalOrderbookService _service;
        
        private LightweightExternalOrderbookService _lightweightService;
        
        public ExternalOrderbookServicesBenchmark()
        {
            var doMock = new Mock<IAssetPairDayOffService>();
            doMock.Setup(a => a.IsDayOff(It.IsAny<string>())).Returns(true);

            _service = new ExternalOrderbookService(
                Mock.Of<IEventChannel<BestPriceChangeEventArgs>>(),
                Mock.Of<IOrderBookProviderApi>(),
                Mock.Of<IDateService>(),
                Mock.Of<IConvertService>(),
                doMock.Object,
                Mock.Of<IAssetPairsCache>(),
                Mock.Of<ICqrsSender>(),
                Mock.Of<IIdentityGenerator>(),
                Mock.Of<ILog>(),
                new MarginTradingSettings() {DefaultExternalExchangeId = "test"});
            
            _lightweightService = new LightweightExternalOrderbookService(
                Mock.Of<IEventChannel<BestPriceChangeEventArgs>>(),
                Mock.Of<IOrderBookProviderApi>(),
                Mock.Of<IDateService>(),
                Mock.Of<IConvertService>(),
                doMock.Object,
                Mock.Of<IAssetPairsCache>(),
                Mock.Of<ICqrsSender>(),
                Mock.Of<IIdentityGenerator>(),
                Mock.Of<ILog>(),
                new MarginTradingSettings() {DefaultExternalExchangeId = "test"});
        }
        
        private static readonly ExternalOrderBook OrderBook = new ExternalOrderBook(
            "test",
            "test",
            DateTime.Now,
            new []
            {
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1}
            },
            new []
            {
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
                new VolumePrice {Price = 1, Volume = 1},
            }
        );

        [Benchmark]
        public void Default()
        {
            _service.SetOrderbook(OrderBook);
        }
        
        [Benchmark]
        public void Lightweight()
        {
            _lightweightService.SetOrderbook(OrderBook);
        }
    }
}