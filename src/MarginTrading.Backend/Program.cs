using System;
using System.IO;
using System.Threading;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Hosting;

#pragma warning disable 1591

namespace MarginTrading.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var restartAttempsLeft = 5;

            while (restartAttempsLeft > 0)
            {
                try
                {
                    var host = new WebHostBuilder()
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseUrls("http://*:5000")
                        .UseStartup<Startup>()
                        .UseApplicationInsights()
                        .Build();

                    host.Run();

                    restartAttempsLeft = 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}{Environment.NewLine}{e.StackTrace}{Environment.NewLine}Restarting...");
                    LogLocator.CommonLog?.WriteFatalErrorAsync(
                        "MT Backend", "Restart host", $"Attempts left: {restartAttempsLeft}", e);
                    restartAttempsLeft--;
                    Thread.Sleep(10000);
                }
            }
        }
    }
}
