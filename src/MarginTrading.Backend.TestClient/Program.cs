using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncFriendlyStackTrace;
using Lykke.HttpClientGenerator;
using Lykke.HttpClientGenerator.Retries;
using MarginTrading.Backend.Contracts;
using Newtonsoft.Json;
using Refit;

namespace MarginTrading.Backend.TestClient
{
    internal static class Program
    {
        private static int _counter;

        static async Task Main(string[] args)
        {
            try
            {
                await Run();
            }
            catch (ApiException e)
            {
                var str = e.Content;
                if (str.StartsWith('"'))
                {
                    str = TryDeserializeToString(str);
                }

                Console.WriteLine(str);
                Console.WriteLine(e.ToAsyncString());
            }
        }

        private static string TryDeserializeToString(string str)
        {
            try
            {
                return JsonConvert.DeserializeObject<string>(str);
            }
            catch
            {
                return str;
            }
        }

        // NOTE: Use Demo instances for tests
        private static async Task Run()
        {
            var retryStrategy = new LinearRetryStrategy(TimeSpan.FromSeconds(10), 50);
            var dataReaderGenerator = HttpClientGenerator.BuildForUrl("http://localhost:5008")
                .WithApiKey("margintrading").WithRetriesStrategy(retryStrategy).Create();

            await CheckTradeMonitoring(dataReaderGenerator);

            Console.WriteLine("Successfuly finished");
        }

        private static async Task CheckTradeMonitoring(HttpClientGenerator dataReaderGenerator)
        {
            var tradeMonitoringApi = dataReaderGenerator.Generate<ITradeMonitoringReadingApi>();

            var assetSumary = await tradeMonitoringApi.AssetSummaryList().Dump();

            var openPositions = await tradeMonitoringApi.OpenPositions().Dump();
            var openPosition = openPositions.FirstOrDefault();
            if (openPosition != null)
            {
                string accountId = openPosition.AccountId;
                var openPositionsByClient = await tradeMonitoringApi.OpenPositionsByClient(new[] {accountId}).Dump();
            }

            var openPositionsByDate =
                await tradeMonitoringApi.OpenPositionsByDate(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow).Dump();
            var openPositionsByVolume = await tradeMonitoringApi.OpenPositionsByVolume(100).Dump();

            var pendingOrders = await tradeMonitoringApi.PendingOrders().Dump();
            var pendingOrder = pendingOrders.FirstOrDefault();
            if (pendingOrder != null)
            {
                string accountId = openPosition.AccountId;
                var pendingOrdersByClient = await tradeMonitoringApi.PendingOrdersByClient(new[] {accountId}).Dump();
            }

            var pendingOrdersByDate =
                await tradeMonitoringApi.PendingOrdersByDate(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow).Dump();
            var pendingOrdersByVolume = await tradeMonitoringApi.PendingOrdersByVolume(100).Dump();

            var orderBooksByInstrument = await tradeMonitoringApi.OrderBooksByInstrument("BTCUSD");
        }

        public static T Dump<T>(this T o)
        {
            var str = o is string s ? s : JsonConvert.SerializeObject(o);
            Console.WriteLine("{0}. {1}", ++_counter, str);
            return o;
        }

        public static async Task<T> Dump<T>(this Task<T> t)
        {
            return (await t).Dump();
        }

        public static async Task Dump(this Task o)
        {
            await o;
            "ok".Dump();
        }
    }
}