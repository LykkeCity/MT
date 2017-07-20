using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Flurl.Http;
using Lykke.Logs;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories;
using MarginTrading.Common.Extensions;
using MarginTrading.Core;
using MarginTrading.Core.Monitoring;
using MarginTrading.Core.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace MarginTrading.OrderRejectedBroker
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }

        public Startup(IHostingEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.dev.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            ILoggerFactory loggerFactory = new LoggerFactory()
                .AddConsole(LogLevel.Error)
                .AddDebug(LogLevel.Warning);

            services.AddSingleton(loggerFactory);
            services.AddLogging();
            services.AddSingleton(Configuration);
            services.AddMvc();

            var builder = new ContainerBuilder();

            MtBackendSettings mtSettings = Environment.IsDevelopment()
                ? Configuration.Get<MtBackendSettings>()
                : SettingsProcessor.Process<MtBackendSettings>(Configuration["SettingsUrl"].GetStringAsync().Result);

            bool isLive = Configuration.IsLive();
            MarginSettings settings = isLive ? mtSettings.MtBackend.MarginTradingLive : mtSettings.MtBackend.MarginTradingDemo;
            settings.IsLive = isLive;

            Console.WriteLine($"IsLive: {settings.IsLive}");

            RegisterRepositories(builder, settings);

            builder.RegisterInstance(settings).SingleInstance();
            builder.RegisterType<Application>()
                .AsSelf()
                .SingleInstance();

            builder.Populate(services);
            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            app.UseMvc();

            Application application = app.ApplicationServices.GetService<Application>();

            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());

            appLifetime.ApplicationStarted.Register(() =>
                application.RunAsync().Wait()
            );

            appLifetime.ApplicationStopping.Register(() =>
                {
                    application.StopApplication();
                }
            );
        }

        private void RegisterRepositories(ContainerBuilder builder, MarginSettings settings)
        {
            LykkeLogToAzureStorage log = new LykkeLogToAzureStorage(PlatformServices.Default.Application.ApplicationName,
                new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "MarginTradingOrderRejectedBrokerLog", null));

            builder.RegisterInstance((ILog)log)
                .As<ILog>()
                .SingleInstance();

            builder.Register<IMarginTradingOrdersRejectedRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateOrdersRejectedRepository(settings.Db.HistoryConnString, log)
            ).SingleInstance();
        }
    }
}
