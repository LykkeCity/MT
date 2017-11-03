using System;
using System.Collections.Generic;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Flurl.Http;
using Lykke.AzureQueueIntegration;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Filters;
using MarginTrading.Backend.Infrastructure;
using MarginTrading.Backend.Middleware;
using MarginTrading.Backend.Modules;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Modules;
using MarginTrading.Backend.Services.Settings;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Json;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.Swagger.Model;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

#pragma warning disable 1591

namespace MarginTrading.Backend
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
                .AddDebug(LogLevel.Error);

            services.AddSingleton(loggerFactory);
            services.AddLogging();
            services.AddSingleton(Configuration);
            services.AddMvc(options => options.Filters.Add(typeof(MarginTradingEnabledFilter)))
                .AddJsonOptions(
                    options =>
                    {
                        options.SerializerSettings.Converters = SerializerSettings.GetDefaultConverters();
                    });
            services.AddAuthentication(KeyAuthOptions.AuthenticationScheme)
                .AddScheme<KeyAuthOptions, KeyAuthHandler>(KeyAuthOptions.AuthenticationScheme, "", options => { });

            bool isLive = Configuration.IsLive();

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", $"MarginTrading_Api_{(isLive ? "Live" : "Demo")}");
                options.OperationFilter<ApiKeyHeaderOperationFilter>();
            });

            var builder = new ContainerBuilder();

            MtBackendSettings mtSettings = Environment.IsDevelopment()
                ? Configuration.Get<MtBackendSettings>()
                : SettingsProcessor.Process<MtBackendSettings>(Configuration["SettingsUrl"].GetStringAsync().Result);

            MarginSettings settings = isLive ? mtSettings.MtBackend.MarginTradingLive : mtSettings.MtBackend.MarginTradingDemo;
            settings.IsLive = isLive;
            settings.Env = isLive ? "Live" : "Demo";
            
            if (!string.IsNullOrEmpty(Configuration["Env"]))
            {
                settings.Env += "." + Configuration["Env"];
            }
            
            Console.WriteLine($"IsLive: {settings.IsLive}");

            SetupLoggers(services, mtSettings, settings);

            RegisterModules(builder, mtSettings, settings, Environment);

            builder.Populate(services);
            ApplicationContainer = builder.Build();

            var meRepository = ApplicationContainer.Resolve<IMatchingEngineRepository>();
            meRepository.InitMatchingEngines(new List<IMatchingEngineBase>
            {
                ApplicationContainer.Resolve<IInternalMatchingEngine>(),
                new RejectMatchingEngine()
            });

            MtServiceLocator.FplService = ApplicationContainer.Resolve<IFplService>();
            MtServiceLocator.AccountUpdateService = ApplicationContainer.Resolve<IAccountUpdateService>();
            MtServiceLocator.AccountsCacheService = ApplicationContainer.Resolve<IAccountsCacheService>();
            MtServiceLocator.SwapCommissionService = ApplicationContainer.Resolve<ICommissionService>();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            app.UseMiddleware<GlobalErrorHandlerMiddleware>();
            app.UseMiddleware<MaintenanceModeMiddleware>();
            app.UseAuthentication();
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUi();

            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());

            Application application = app.ApplicationServices.GetService<Application>();

            var settings = app.ApplicationServices.GetService<MarginSettings>();

            appLifetime.ApplicationStarted.Register(() =>
            {
                if (!string.IsNullOrEmpty(settings.ApplicationInsightsKey))
                {
                    Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.InstrumentationKey =
                        settings.ApplicationInsightsKey;
                }
            });

            appLifetime.ApplicationStopping.Register(() =>
                application.StopApplication()
            );
        }

        private void RegisterModules(ContainerBuilder builder, MtBackendSettings mtSettings, MarginSettings settings, IHostingEnvironment environment)
        {
            builder.RegisterModule(new BackendSettingsModule(mtSettings, settings));
            builder.RegisterModule(new BackendRepositoriesModule(settings, LogLocator.CommonLog));
            builder.RegisterModule(new EventModule());
            builder.RegisterModule(new CacheModule());
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new BaseServicesModule(mtSettings));
            builder.RegisterModule(new ServicesModule());
            builder.RegisterModule(new BackendServicesModule(mtSettings, settings, environment, LogLocator.CommonLog));

            builder.RegisterBuildCallback(c => c.Resolve<AccountAssetsManager>());
            builder.RegisterBuildCallback(c => c.Resolve<OrderBookSaveService>());
            builder.RegisterBuildCallback(c => c.Resolve<MicrographManager>());
            builder.RegisterBuildCallback(c => c.Resolve<QuoteCacheService>());
            builder.RegisterBuildCallback(c => c.Resolve<OrderCacheManager>());
            builder.RegisterBuildCallback(c => c.Resolve<PendingOrdersCleaningService>());
        }

        private static void SetupLoggers(IServiceCollection services, MtBackendSettings mtSettings,
            MarginSettings settings)
        {
            var consoleLogger = new LogToConsole();

            var azureQueue = new AzureQueueSettings
            {
                ConnectionString = mtSettings.SlackNotifications.AzureQueue.ConnectionString,
                QueueName = mtSettings.SlackNotifications.AzureQueue.QueueName
            };
            
            var commonSlackService =
                services.UseSlackNotificationsSenderViaAzureQueue(azureQueue, consoleLogger);

            var slackService =
                new MtSlackNotificationsSender(commonSlackService, "MT Backend", settings.Env);

            // Order of logs registration is important - UseLogToAzureStorage() registers ILog in container. 
            // Last registration wins.
            LogLocator.RequestsLog = services.UseLogToAzureStorage(settings.Db.LogsConnString,
                slackService, "MarginTradingBackendRequestsLog", consoleLogger);

            LogLocator.CommonLog = services.UseLogToAzureStorage(settings.Db.LogsConnString,
                slackService, "MarginTradingBackendLog", consoleLogger);
        }
    }
}
