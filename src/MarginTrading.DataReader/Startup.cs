using System;
using System.Collections.Generic;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Flurl.Http;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using MarginTrading.Common.Extensions;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using MarginTrading.Services;
using MarginTrading.Services.Infrastructure;
using MarginTrading.Services.Modules;
using MarginTrading.Services.Notifications;
using MarginTrading.Services.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MarginTrading.DataReader.Filters;
using MarginTrading.DataReader.Infrastructure;
using MarginTrading.DataReader.Middleware;
using MarginTrading.DataReader.Modules;
using Microsoft.AspNetCore.Http;
using Swashbuckle.Swagger.Model;

#pragma warning disable 1591

namespace MarginTrading.DataReader
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

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            ILoggerFactory loggerFactory = new LoggerFactory()
                .AddConsole(LogLevel.Error)
                .AddDebug(LogLevel.Error);

            services.AddSingleton(loggerFactory);
            services.AddLogging();
            services.AddSingleton(Configuration);
            services.AddMvc(options => options.Filters.Add(typeof(MarginTradingEnabledFilter)));
            services.AddAuthentication(KeyAuthOptions.AuthenticationScheme)
                .AddScheme<KeyAuthOptions, KeyAuthHandler>(KeyAuthOptions.AuthenticationScheme, "", options => { });

            bool isLive = Configuration.IsLive();

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", $"MarginTrading_DataReader_Api_{(isLive ? "Live" : "Demo")}");
                options.OperationFilter<ApiKeyHeaderOperationFilter>();
                options.OperationFilter<CustomOperationIdOperationFilter>();
            });

            var builder = new ContainerBuilder();

            MtBackendSettings mtSettings = Environment.IsDevelopment()
                ? Configuration.Get<MtBackendSettings>()
                : SettingsProcessor.Process<MtBackendSettings>(Configuration["SettingsUrl"].GetStringAsync().Result);

            MarginSettings settings = isLive ? mtSettings.MtBackend.MarginTradingLive : mtSettings.MtBackend.MarginTradingDemo;
            settings.IsLive = isLive;
            settings.Env = isLive ? "Live" : "Demo";

            Console.WriteLine($"IsLive: {settings.IsLive}");

            SetupLoggers(services, mtSettings, settings);

            RegisterModules(builder, settings);

            builder.Populate(services);
            ApplicationContainer = builder.Build();
            return new AutofacServiceProvider(ApplicationContainer);
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            app.UseLykkeMiddleware("MarginTradingDataReader",
#if DEBUG
                ex => new { ErrorMessage = ex.ToString() });
#else
                ex => new { ErrorMessage = "Technical problem" });
#endif
            app.UseAuthentication();
            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUi();

            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());

            var settings = app.ApplicationServices.GetService<MarginSettings>();

            appLifetime.ApplicationStarted.Register(() =>
            {
                if (!string.IsNullOrEmpty(settings.ApplicationInsightsKey))
                {
                    Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.InstrumentationKey =
                        settings.ApplicationInsightsKey;
                }
            });

            appLifetime.ApplicationStopping.Register(() => { });
        }

        private void RegisterModules(ContainerBuilder builder, MarginSettings settings)
        {
            builder.RegisterModule(new DataReaderSettingsModule(settings));
            builder.RegisterModule(new DataReaderRepositoriesModule(settings, LogLocator.CommonLog));
            builder.RegisterModule(new DataReaderServicesModule());
        }

        private static void SetupLoggers(IServiceCollection services, MtBackendSettings mtSettings,
            MarginSettings settings)
        {
            var consoleLogger = new LogToConsole();

            var commonSlackService =
                services.UseSlackNotificationsSenderViaAzureQueue(mtSettings.SlackNotifications.AzureQueue,
                    new LogToConsole());

            var slackService =
                new MtSlackNotificationsSender(commonSlackService, "MT DataReader", settings.Env);

            var log = services.UseLogToAzureStorage(settings.Db.LogsConnString, slackService,
                "MarginTradingDataReaderLog", consoleLogger);

            LogLocator.CommonLog = log;
        }
    }
}
