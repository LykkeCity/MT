using System;
using System.Threading;
using MarginTrading.Services.Infrastructure;
using Microsoft.AspNetCore.Hosting;

#pragma warning disable 1591

namespace MarginTrading.DataReader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var restartAttempsLeft = 5;

            while (restartAttempsLeft >= 0)
            {
                try
                {
                    var host = new WebHostBuilder()
                        .UseKestrel()
                        .UseUrls("http://*:5008")
                        .UseStartup<Startup>()
                        .UseApplicationInsights()
                        .Build();

                    host.Run();
                }
                catch (Exception e)
                {
                    LogLocator.CommonLog.WriteFatalErrorAsync(
                        "MT DataReader", "Restart host", $"Attempts left: {restartAttempsLeft}", e);
                    restartAttempsLeft--;
                    Console.WriteLine($"Error: {e.Message}{Environment.NewLine}Restarting...");
                    Thread.Sleep(10000);
                }
            }
        }
    }
}
