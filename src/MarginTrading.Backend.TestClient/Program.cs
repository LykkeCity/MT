using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using MarginTrading.Backend.Contracts.Client;
using MarginTrading.Backend.Contracts.DayOffSettings;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace MarginTrading.Backend.TestClient
{
    internal static class Program
    {
        private static int _counter;

        static async Task Main()
        {
            var services = new ServiceCollection();
            var builder = new ContainerBuilder();
            services.RegisterMtMarketMakerClient("http://localhost:5000", "margintrading", "TestClient");
            builder.Populate(services);
            var container = builder.Build();
            var client = container.Resolve<IMtBackendClient>();
            
            await client.DayOffExclusions.List().Dump();
            var id = Guid.NewGuid();
            await client.DayOffExclusions.Create(new DayOffExclusionContract
            {
                Id = id,
                AssetPairRegex = "lol",
                Start = DateTime.Now.AddDays(-1),
                End = DateTime.Now.Date,
                IsTradeEnabled = false,
            }).Dump();
            var ex = await client.DayOffExclusions.Get(id).Dump();
            ex.AssetPairRegex = "^btc";
            await client.DayOffExclusions.Update(ex).Dump();
            await client.DayOffExclusions.Get(id).Dump();
            await client.DayOffExclusions.ListCompiled().Dump();
            await client.DayOffExclusions.Delete(id).Dump();
            await client.DayOffExclusions.Get(id).Dump();

            var s = await client.ScheduleSettings.Get().Dump();
            s.AssetPairsWithoutDayOff.Add("BTCRABBIT");
            await client.ScheduleSettings.Set(s).Dump();
            s.AssetPairsWithoutDayOff.Remove("BTCRABBIT");
            await client.ScheduleSettings.Set(s).Dump();
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