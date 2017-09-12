using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.BrokerBase
{
    public class WebAppProgramBase<TStartup> where TStartup : class
    {
        protected static void RunOnPort(short listeningPort)
        {
            void RunHost() =>
                new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls("http://*:" + listeningPort)
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<TStartup>()
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
                    restartAttempsLeft = 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Error: {e.Message}{Environment.NewLine}{e.StackTrace}{Environment.NewLine}Restarting (attempts left: {restartAttempsLeft})...");
                    restartAttempsLeft--;
                    Thread.Sleep(10000);
                }
            }
        }
    }
}