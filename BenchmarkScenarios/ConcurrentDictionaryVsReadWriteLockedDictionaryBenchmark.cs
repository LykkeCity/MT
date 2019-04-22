using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Common.Helpers;

namespace BenchmarkScenarios
{
    [CoreJob]
    [RPlotExporter, RankColumn]
    public class ConcurrentDictionaryVsReadWriteLockedDictionaryBenchmark
    {
        private static readonly ReadWriteLockedDictionary<string, Dictionary<string, ExternalOrderBook>> RwlDictionary =
            new ReadWriteLockedDictionary<string, Dictionary<string, ExternalOrderBook>>();

        private static readonly ConcurrentDictionary<string, Dictionary<string, ExternalOrderBook>> ConcurDictionary = 
            new ConcurrentDictionary<string, Dictionary<string, ExternalOrderBook>>();

        private static readonly ExternalOrderBook OrderBook = new ExternalOrderBook(
            "test",
            "test",
            DateTime.Now,
            new [] {new VolumePrice() {Price = 1, Volume = 1}},
            new [] {new VolumePrice() {Price = 1, Volume = 1}}
        );

        private static readonly Action RwlActionAdd = () =>
        {
            RwlDictionary.AddOrUpdate("test",
                k => UpdateOrderbooksDictionary(k, new Dictionary<string, ExternalOrderBook>()),
                UpdateOrderbooksDictionary);
        };
        
        private static readonly Action RwlActionGet = () =>
        {
            RwlDictionary.TryReadValue("test", (dataExist, assetPair, orderbooks)
                => dataExist ? DoSomeJob(orderbooks) : null);
        };

        private static readonly Action ConcurActionAdd = () =>
        {
            ConcurDictionary.AddOrUpdate("test",
                k => UpdateOrderbooksDictionary(k, new Dictionary<string, ExternalOrderBook>()),
                UpdateOrderbooksDictionary);
        };
        
        private static readonly Action ConcurActionGet = () =>
        {
            if (ConcurDictionary.TryGetValue("test", out var orderbooks))
            {
                DoSomeJob(orderbooks);
            }
        };
        
        private readonly List<Action> _rwlDictionaryActions = new List<Action>
        {
            RwlActionAdd,
            RwlActionGet,
            RwlActionGet,
            RwlActionGet,
            RwlActionGet,
        };

        private readonly List<Action> _concurDictionaryActions = new List<Action>
        {
            ConcurActionAdd,
            ConcurActionGet,
            ConcurActionGet,
            ConcurActionGet,
            ConcurActionGet,
        };
        
        private static decimal? DoSomeJob(Dictionary<string, ExternalOrderBook> orderbooks)
        {
            return !orderbooks.TryGetValue("test", out var orderBook)
                ? null
                : orderBook.Asks.FirstOrDefault()?.Price;
        }
        
        private static Dictionary<string, ExternalOrderBook> UpdateOrderbooksDictionary(string assetPairId,
            Dictionary<string, ExternalOrderBook> dict)
        {
            dict[OrderBook.ExchangeName] = OrderBook;
            
            return dict;
        }

        [Benchmark]
        public void RwlDictionaryTest()
        {
            RwlActionAdd();
            RwlActionGet();
        }
        
        [Benchmark]
        public void ConcurDictionaryTest()
        {
            ConcurActionAdd();
            ConcurActionGet();
        }
    }
}