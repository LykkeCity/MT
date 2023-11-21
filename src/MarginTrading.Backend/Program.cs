// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using JetBrains.Annotations;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.PlatformAbstractions;

#pragma warning disable 1591

namespace MarginTrading.Backend
{
    [UsedImplicitly]
    public class Program
    {
        internal static IHost AppHost { get; private set; }

        public static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                LogLocator.CommonLog?.WriteFatalErrorAsync("UnhandledException", nameof(AppDomain), (Exception)e.ExceptionObject);
            };

            Console.WriteLine($@"{PlatformServices.Default.Application.ApplicationName} version {PlatformServices.Default.Application.ApplicationVersion}");

            var restartAttemptsLeft = int.TryParse(Environment.GetEnvironmentVariable("RESTART_ATTEMPTS_NUMBER"),
                out var restartAttemptsFromEnv)
                ? restartAttemptsFromEnv
                : int.MaxValue;
            var restartAttemptsInterval = int.TryParse(Environment.GetEnvironmentVariable("RESTART_ATTEMPTS_INTERVAL_MS"),
                out var restartAttemptsIntervalFromEnv)
                ? restartAttemptsIntervalFromEnv
                : 10000;

            bool fatalErrorOccured;
            do
            {
                try
                {
                    fatalErrorOccured = false;

                    var configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", optional: true)
                        .AddUserSecrets<Startup>()
                        .AddEnvironmentVariables()
                        .Build();

                    AppHost = Host.CreateDefaultBuilder(args)
                        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                        .ConfigureWebHostDefaults(webBuilder =>
                        {
                            webBuilder.ConfigureKestrel(serverOptions =>
                                {
                                    // Set properties and call methods on options
                                })
                                .UseConfiguration(configuration)
                                .UseStartup<Startup>();
                        })
                        .Build();

                    await AppHost.RunAsync();
                }
                catch (Exception e)
                {
                    fatalErrorOccured = true;
                    Console.WriteLine(
                        $@"Error: {e.Message}{Environment.NewLine}{e.StackTrace}{Environment.NewLine}Restarting...");
                    LogLocator.CommonLog?.WriteFatalErrorAsync(
                        "MT Backend", "Restart host", $"Attempts left: {restartAttemptsLeft}", e);
                    restartAttemptsLeft--;
                    Thread.Sleep(restartAttemptsInterval);
                }
            } while (restartAttemptsLeft > 0 && fatalErrorOccured);

            Console.WriteLine(@"Terminated");
        }
    }
}
