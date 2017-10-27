using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.MarketMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"{Startup.ServiceName} version {Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion}");
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif
            Console.WriteLine($"ENV_INFO: {Environment.GetEnvironmentVariable("ENV_INFO")}");

            void RunHost() =>
                new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls("http://*:5007")
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .UseApplicationInsights()
                    .Build()
                    .Run();

            StartWithRetries(RunHost);
        }

        private static void StartWithRetries(Action runHost)
        {
            var restartAttempsLeft = 5;
            while (restartAttempsLeft > 0)
            {
                try
                {
                    runHost();
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}{Environment.NewLine}{e.StackTrace}{Environment.NewLine}Restarting (attempts left: {restartAttempsLeft})...");
                    restartAttempsLeft--;
                    Thread.Sleep(10000);
                }
            }

            if (restartAttempsLeft <= 0)
            {
                Console.WriteLine("Fatal error, retries count exceeded.");

                // Lets devops to see startup error in console between restarts in the Kubernetes
                var delay = TimeSpan.FromMinutes(1);

                Console.WriteLine();
                Console.WriteLine($"Process will be terminated in {delay}. Press any key to terminate immediately.");

                Task.WhenAny(
                        Task.Delay(delay),
                        Task.Run(() => Console.ReadKey(true)))
                    .Wait();
            }

            Console.WriteLine("Terminated");
        }
    }
}