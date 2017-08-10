using System;
using System.IO;
using System.Threading;
using MarginTrading.Services.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace MarginTrading.Frontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cfgBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables();

            var configuration = cfgBuilder.Build();

            int kestrelThreadsCount = 1;
            string threadsCount = configuration["KestrelThreadCount"];

            if (threadsCount != null)
            {
                if (!int.TryParse(threadsCount, out kestrelThreadsCount))
                {
                    Console.WriteLine($"Can't parse KestrelThreadsCount value '{threadsCount}'");
                    return;
                }
            }

            Console.WriteLine($"Kestrel threads count: {kestrelThreadsCount}");

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
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseIISIntegration()
                        .UseUrls("http://*:5005")
                        .UseStartup<Startup>()
                        .UseApplicationInsights()
                        .Build();

                    host.Run();
                }
                catch (Exception e)
                {
                    LogLocator.CommonLog.WriteFatalErrorAsync(
                        "MT Frontend", "Restart host", $"Attempts left: {restartAttempsLeft}", e);
                    restartAttempsLeft--;
                    Console.WriteLine($"Error: {e.Message}{Environment.NewLine}Restarting...");
                    Thread.Sleep(10000);
                }
            }
        }
    }
}
