using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.MarketMaker
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif

            new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5007")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build()
                .Run();
        }
    }
}