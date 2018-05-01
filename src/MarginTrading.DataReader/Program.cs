using System;
using System.IO;
using System.Threading;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.PlatformAbstractions;

#pragma warning disable 1591

namespace MarginTrading.DataReader
{
    public class Program
    {
        public static string EnvInfo => Environment.GetEnvironmentVariable("ENV_INFO") ?? "DefaultEnv";

        public static void Main(string[] args)
        {
            Console.WriteLine($"{PlatformServices.Default.Application.ApplicationName} version " +
                              PlatformServices.Default.Application.ApplicationVersion);
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif
            Console.WriteLine($"ENV_INFO: {EnvInfo}");


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
                    Console.WriteLine(
                        $"Error: {e.Message}{Environment.NewLine}{e.StackTrace}{Environment.NewLine}Restarting...");
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