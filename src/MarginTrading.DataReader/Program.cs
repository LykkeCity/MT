using System;
using System.IO;
using System.Threading;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Hosting;

#pragma warning disable 1591

namespace MarginTrading.DataReader
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
                        .UseIISIntegration()
                        .UseUrls("http://*:5008")
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
                        "MT DataReader", "Restart host", $"Attempts left: {restartAttempsLeft}", e);
                    restartAttempsLeft--;
                    Console.WriteLine($"Error: {e.Message}{Environment.NewLine}Restarting...");
                    Thread.Sleep(10000);
                }
            }
        }
    }
}
