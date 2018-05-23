using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.AzureQueueIntegration;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Lykke.SlackNotifications;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Filters;
using MarginTrading.Backend.Infrastructure;
using MarginTrading.Backend.Middleware;
using MarginTrading.Backend.Modules;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Modules;
using MarginTrading.Backend.Services.Quotes;
using MarginTrading.Backend.Services.Settings;
using MarginTrading.Backend.Services.Stubs;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Json;
using MarginTrading.Common.Modules;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
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
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });
            services.AddAuthentication(KeyAuthOptions.AuthenticationScheme)
                .AddScheme<KeyAuthOptions, KeyAuthHandler>(KeyAuthOptions.AuthenticationScheme, "", options => { });

            var isLive = Configuration.IsLive();

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", $"MarginTradingEngine_Api_{Configuration.ServerType()}");
                options.OperationFilter<ApiKeyHeaderOperationFilter>();
            });

            var builder = new ContainerBuilder();

            var envSuffix = !string.IsNullOrEmpty(Configuration["Env"]) ? "." + Configuration["Env"] : "";
            var mtSettings = Configuration.LoadSettings<MtBackendSettings>()
                .Nested(s =>
                {
                    s.MtBackend.IsLive = isLive;
                    s.MtBackend.Env = Configuration.ServerType() + envSuffix;
                    return s;
                });

            var settings = mtSettings.Nested(s => s.MtBackend);
            
            Console.WriteLine($"IsLive: {settings.CurrentValue.IsLive}");

            SetupLoggers(services, mtSettings, settings);

            RegisterModules(builder, mtSettings, settings, Environment);

            builder.Populate(services);
            
            ApplicationContainer = builder.Build();

            MtServiceLocator.FplService = ApplicationContainer.Resolve<IFplService>();
            MtServiceLocator.AccountUpdateService = ApplicationContainer.Resolve<IAccountUpdateService>();
            MtServiceLocator.AccountsCacheService = ApplicationContainer.Resolve<IAccountsCacheService>();
            MtServiceLocator.SwapCommissionService = ApplicationContainer.Resolve<ICommissionService>();

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

            appLifetime.ApplicationStarted.Register(() =>
            {
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
            IReloadingManager<MarginTradingSettings> settings, IHostingEnvironment environment)
        {
            builder.RegisterModule(new BaseServicesModule(mtSettings.CurrentValue, LogLocator.CommonLog));
            builder.RegisterModule(new BackendSettingsModule(mtSettings));
            builder.RegisterModule(new BackendRepositoriesModule(settings, LogLocator.CommonLog));
            builder.RegisterModule(new EventModule());
            builder.RegisterModule(new CacheModule());
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new ServicesModule());
            builder.RegisterModule(new BackendServicesModule(mtSettings.CurrentValue, settings.CurrentValue,
                environment, LogLocator.CommonLog));
            builder.RegisterModule(new MarginTradingCommonModule());
            builder.RegisterModule(new ExternalServicesModule(mtSettings));
            builder.RegisterModule(new BackendMigrationsModule());
            builder.RegisterModule(new CqrsModule(settings.CurrentValue.Cqrs, LogLocator.CommonLog));
            builder.RegisterModule(new FakeExchangeConnectorModule(LogLocator.CommonLog));

            builder.RegisterBuildCallback(c => c.Resolve<TradingInstrumentsManager>());
            builder.RegisterBuildCallback(c => c.Resolve<OrderBookSaveService>());
            builder.RegisterBuildCallback(c => c.Resolve<QuoteCacheService>());
            builder.RegisterBuildCallback(c => c.Resolve<AccountManager>()); // note the order here is important!
            builder.RegisterBuildCallback(c => c.Resolve<OrderCacheManager>());
            builder.RegisterBuildCallback(c => c.Resolve<PendingOrdersCleaningService>());
        }

        private static void SetupLoggers(IServiceCollection services, IReloadingManager<MtBackendSettings> mtSettings,
            IReloadingManager<MarginTradingSettings> settings)
        {
            var consoleLogger = new LogToConsole();

            IMtSlackNotificationsSender slackService = null;
            
            if (mtSettings.CurrentValue.SlackNotifications != null)
            {
                var azureQueue = new AzureQueueSettings
                {
                    ConnectionString = mtSettings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                    QueueName = mtSettings.CurrentValue.SlackNotifications.AzureQueue.QueueName
                };

                var commonSlackService =
                    services.UseSlackNotificationsSenderViaAzureQueue(azureQueue, consoleLogger);

                slackService =
                    new MtSlackNotificationsSender(commonSlackService, "MT Backend", settings.CurrentValue.Env);
            }
            else
            {
                slackService =
                    new MtSlackNotificationsSenderLogStub("MT Backend", settings.CurrentValue.Env, consoleLogger);
            }
            
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