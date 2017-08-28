using System;
using System.IO;
using System.Threading;
using MarginTrading.Services.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.Frontend
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
