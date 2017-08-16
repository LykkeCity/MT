using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.MarketMaker.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif
            var webHostCancellationTokenSource = new CancellationTokenSource();
            var end = new ManualResetEvent(false);

            AssemblyLoadContext.Default.Unloading += ctx =>
            {
                Console.WriteLine("SIGTERM recieved");

                webHostCancellationTokenSource.Cancel();

                end.WaitOne();
            };

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run(webHostCancellationTokenSource.Token);

            end.Set();

            Console.WriteLine("Terminated");
        }
    }
}
