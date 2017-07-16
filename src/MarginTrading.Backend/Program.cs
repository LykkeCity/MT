using System;
using System.Threading;
using MarginTrading.Services.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

#pragma warning disable 1591

namespace MarginTrading.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cfgBuilder = new ConfigurationBuilder()
              .AddEnvironmentVariables();

            var configuration = cfgBuilder.Build();

            int kestrelThreadsCount = 0;
            string threadsCount = configuration["KestrelThreadCount"];

            if (threadsCount != null)
            {
                if (!int.TryParse(threadsCount, out kestrelThreadsCount))
                {
                    Console.WriteLine($"Can't parse KestrelThreadsCount value '{threadsCount}'");
                    return;
                }

                Console.WriteLine($"Kestrel threads count: {kestrelThreadsCount}");
            }
            else
            {
                Console.WriteLine("KestrelThreadsCount is not set. Using default value");
            }

            var restartAttempsLeft = 5;

            while (restartAttempsLeft >= 0)
            {
                try
                {
                    var host = new WebHostBuilder()
                        .UseKestrel(options =>
                        {
                            if (kestrelThreadsCount > 0)
                            {
                                options.ThreadCount = kestrelThreadsCount;
                            }
                        })
                        .UseUrls("http://*:5000")
                        .UseStartup<Startup>()
                        .Build();

                    host.Run();
                }
                catch (Exception e)
                {
                    LogLocator.CurrentLog.WriteFatalErrorAsync(
                        "MT Backend", "Restart host", $"Attempts left: {restartAttempsLeft}", e);
                    restartAttempsLeft--;
                    Console.WriteLine($"Error: {e.Message}{Environment.NewLine}Restarting...");
                    Thread.Sleep(10000);
                }
            }
        }
    }
}
