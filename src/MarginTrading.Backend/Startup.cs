using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.AzureQueueIntegration;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Lykke.SlackNotifications;
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
using MarginTrading.Backend.Services.Quotes;
using MarginTrading.Backend.Services.Settings;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Json;
using MarginTrading.Common.Modules;
using MarginTrading.Common.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GlobalErrorHandlerMiddleware = MarginTrading.Backend.Middleware.GlobalErrorHandlerMiddleware;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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
                .SetBasePath(env.ContentRootPath)
                .AddDevJson(env)
                .AddEnvironmentVariables()
                .Build();

            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var loggerFactory = new LoggerFactory()
                .AddConsole(LogLevel.Error)
                .AddDebug(LogLevel.Error);

            services.AddSingleton(loggerFactory);
            services.AddLogging();
            services.AddSingleton(Configuration);
            services.AddMvc(options => options.Filters.Add(typeof(MarginTradingEnabledFilter)))
                .AddJsonOptions(
                    options => { options.SerializerSettings.Converters = SerializerSettings.GetDefaultConverters(); });
            services.AddAuthentication(KeyAuthOptions.AuthenticationScheme)
                .AddScheme<KeyAuthOptions, KeyAuthHandler>(KeyAuthOptions.AuthenticationScheme, "", options => { });

            var isLive = Configuration.IsLive();

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", $"MarginTrading_Api_{Configuration.ServerType()}");
                options.OperationFilter<ApiKeyHeaderOperationFilter>();
            });

            var builder = new ContainerBuilder();

            var envSuffix = !string.IsNullOrEmpty(Configuration["Env"]) ? "." + Configuration["Env"] : "";
            var mtSettings = Configuration.LoadSettings<MtBackendSettings>()
                .Nested(s =>
                {
                    var inner = isLive ? s.MtBackend.MarginTradingLive : s.MtBackend.MarginTradingDemo;
                    inner.IsLive = isLive;
                    inner.Env = Configuration.ServerType() + envSuffix;
                    return s;
                });

            var settings =
                mtSettings.Nested(s => isLive ? s.MtBackend.MarginTradingLive : s.MtBackend.MarginTradingDemo);
            var riskInformingSettings =
                mtSettings.Nested(s => isLive ? s.RiskInformingSettings : s.RiskInformingSettingsDemo);

            Console.WriteLine($"IsLive: {settings.CurrentValue.IsLive}");

            SetupLoggers(services, mtSettings, settings);

            RegisterModules(builder, mtSettings, settings, Environment, riskInformingSettings);

            builder.Populate(services);
            
            ApplicationContainer = builder.Build();

            MtServiceLocator.FplService = ApplicationContainer.Resolve<IFplService>();
            MtServiceLocator.AccountUpdateService = ApplicationContainer.Resolve<IAccountUpdateService>();
            MtServiceLocator.AccountsCacheService = ApplicationContainer.Resolve<IAccountsCacheService>();
            MtServiceLocator.SwapCommissionService = ApplicationContainer.Resolve<ICommissionService>();
            MtServiceLocator.OvernightSwapService = ApplicationContainer.Resolve<IOvernightSwapService>();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            IApplicationLifetime appLifetime)
        {
            app.UseMiddleware<GlobalErrorHandlerMiddleware>();
            app.UseMiddleware<MaintenanceModeMiddleware>();
            app.UseAuthentication();
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUi();

            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());
            
            var application = app.ApplicationServices.GetService<Application>();

            var settings = app.ApplicationServices.GetService<MarginSettings>();

            appLifetime.ApplicationStarted.Register(() =>
            {
                if (!string.IsNullOrEmpty(settings.ApplicationInsightsKey))
                {
                    TelemetryConfiguration.Active.InstrumentationKey = settings.ApplicationInsightsKey;
                }

                LogLocator.CommonLog?.WriteMonitorAsync("", "", $"{Configuration.ServerType()} Started");
            });

            appLifetime.ApplicationStopping.Register(() =>
                {
                    LogLocator.CommonLog?.WriteMonitorAsync("", "", $"{Configuration.ServerType()} Terminating");
                    application.StopApplication();
                }
            );
        }

        private void RegisterModules(ContainerBuilder builder, IReloadingManager<MtBackendSettings> mtSettings,
            IReloadingManager<MarginSettings> settings, IHostingEnvironment environment,
            IReloadingManager<RiskInformingSettings> riskInformingSettings)
        {
            builder.RegisterModule(new BaseServicesModule(mtSettings.CurrentValue, LogLocator.CommonLog));
            builder.RegisterModule(new BackendSettingsModule(mtSettings.CurrentValue, settings));
            builder.RegisterModule(new BackendRepositoriesModule(settings, LogLocator.CommonLog));
            builder.RegisterModule(new EventModule());
            builder.RegisterModule(new CacheModule());
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new ServicesModule(riskInformingSettings));
            builder.RegisterModule(new BackendServicesModule(mtSettings.CurrentValue, settings.CurrentValue,
                environment, LogLocator.CommonLog));
            builder.RegisterModule(new MarginTradingCommonModule());
            builder.RegisterModule(new ExternalServicesModule(mtSettings));
            builder.RegisterModule(new BackendMigrationsModule());

            builder.RegisterBuildCallback(c => c.Resolve<AccountAssetsManager>());
            builder.RegisterBuildCallback(c => c.Resolve<OrderBookSaveService>());
            builder.RegisterBuildCallback(c => c.Resolve<MicrographManager>());
            builder.RegisterBuildCallback(c => c.Resolve<QuoteCacheService>());
            builder.RegisterBuildCallback(c => c.Resolve<AccountManager>()); // note the order here is important!
            builder.RegisterBuildCallback(c => c.Resolve<OrderCacheManager>());
            builder.RegisterBuildCallback(c => c.Resolve<PendingOrdersCleaningService>());
            builder.RegisterBuildCallback(c => c.Resolve<IOvernightSwapService>());
        }

        private static void SetupLoggers(IServiceCollection services, IReloadingManager<MtBackendSettings> mtSettings,
            IReloadingManager<MarginSettings> settings)
        {
            var consoleLogger = new LogToConsole();

            var azureQueue = new AzureQueueSettings
            {
                ConnectionString = mtSettings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                QueueName = mtSettings.CurrentValue.SlackNotifications.AzureQueue.QueueName
            };

            var commonSlackService =
                services.UseSlackNotificationsSenderViaAzureQueue(azureQueue, consoleLogger);

            var slackService =
                new MtSlackNotificationsSender(commonSlackService, "MT Backend", settings.CurrentValue.Env);

            services.AddSingleton<ISlackNotificationsSender>(slackService);
            services.AddSingleton<IMtSlackNotificationsSender>(slackService);

            // Order of logs registration is important - UseLogToAzureStorage() registers ILog in container.
            // Last registration wins.
            LogLocator.RequestsLog = services.UseLogToAzureStorage(settings.Nested(s => s.Db.LogsConnString),
                slackService, "MarginTradingBackendRequestsLog", consoleLogger);

            LogLocator.CommonLog = services.UseLogToAzureStorage(settings.Nested(s => s.Db.LogsConnString),
                slackService, "MarginTradingBackendLog", consoleLogger);
        }
    }
}