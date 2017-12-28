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
            
            await client.ScheduleSettings.ListExclusions().Dump();
            var excl = await client.ScheduleSettings.CreateExclusion(new DayOffExclusionInputContract
            {
                AssetPairRegex = "lol",
                Start = DateTime.Now.AddDays(-1),
                End = DateTime.Now.Date,
                IsTradeEnabled = false,
            }).Dump();
            var id = excl.Id;
            var ex = await client.ScheduleSettings.GetExclusion(id).Dump();
            ex.AssetPairRegex = "^btc";
            await client.ScheduleSettings.UpdateExclusion(id, ex).Dump();
            await client.ScheduleSettings.GetExclusion(id).Dump();
            await client.ScheduleSettings.ListCompiledExclusions().Dump();
            await client.ScheduleSettings.DeleteExclusion(id).Dump();
            await client.ScheduleSettings.GetExclusion(id).Dump();

            var s = await client.ScheduleSettings.GetSchedule().Dump();
            s.AssetPairsWithoutDayOff.Add("BTCRABBIT");
            await client.ScheduleSettings.SetSchedule(s).Dump();
            s.AssetPairsWithoutDayOff.Remove("BTCRABBIT");
            await client.ScheduleSettings.SetSchedule(s).Dump();
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