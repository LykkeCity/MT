using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncFriendlyStackTrace;
using Lykke.HttpClientGenerator;
using Lykke.HttpClientGenerator.Retries;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Prices;
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
            var generator = HttpClientGenerator.BuildForUrl("http://localhost:5000").WithApiKey("margintrading")
                .WithRetriesStrategy(retryStrategy).Create();

            await CheckPositionsAsync(generator);
            
            //await CheckAccountsAsync(generator);
            //await CheckOrdersAsync(generator);
            //await CheckPricesAsync(generator);

            Console.WriteLine("Successfuly finished");
        }

        private static async Task CheckPricesAsync(HttpClientGenerator generator)
        {
            var api = generator.Generate<IPricesApi>();
            await api.GetBestAsync(new InitPricesBackendRequest {AssetIds = new[] {"EURUSD"}}).Dump();
        }
        
        private static async Task CheckOrdersAsync(HttpClientGenerator generator)
        {
            var api = generator.Generate<IOrdersApi>();
            await api.ListAsync().Dump();
        }

        private static async Task CheckPositionsAsync(HttpClientGenerator generator)
        {
            var api = generator.Generate<IPositionsApi>();
            var positions = await api.ListAsync().Dump();
            var anyPosition = positions.FirstOrDefault();
            if (anyPosition != null)
            {
                await api.CloseAsync(anyPosition.Id/*,
                    new PositionCloseRequest {Comment = "111", Originator = OriginatorTypeContract.Investor}*/).Dump();
            }
        }

        private static async Task CheckAccountsAsync(HttpClientGenerator generator)
        {
            var api = generator.Generate<IAccountsApi>();
            await api.GetAllAccountStats().Dump();
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