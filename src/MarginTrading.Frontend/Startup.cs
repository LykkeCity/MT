using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Flurl.Http;
using Lykke.Common;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.Session;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Settings;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Common.Json;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.RabbitMqMessageModels;
using MarginTrading.Common.Wamp;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Settings;
using MarginTrading.Frontend.Infrastructure;
using MarginTrading.Frontend.Middleware;
using MarginTrading.Frontend.Services;
using MarginTrading.Frontend.Settings;
using MarginTrading.Services;
using MarginTrading.Services.Infrastructure;
using MarginTrading.Services.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Rocks.Caching;
using WampSharp.AspNetCore.WebSockets.Server;
using WampSharp.Binding;
using WampSharp.V2;
using WampSharp.V2.MetaApi;
using WampSharp.V2.Realm;
#pragma warning disable 1591

namespace MarginTrading.Frontend
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }

        private readonly TimeSpan _subscriberRetryTimeout = TimeSpan.FromSeconds(1);

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
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    options.SerializerSettings.Converters = SerializerSettings.GetDefaultConverters();
                });

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", "MarginTrading_Api");
                options.OperationFilter<AddAuthorizationHeaderParameter>();
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.SecurityTokenValidators.Clear();
                    options.SecurityTokenValidators.Add(ApplicationContainer.Resolve<ISecurityTokenValidator>());
                });

            var builder = new ContainerBuilder();

            ApplicationSettings appSettings = Environment.IsDevelopment()
                ? Configuration.Get<ApplicationSettings>()
                : SettingsProcessor.Process<ApplicationSettings>(Configuration["SettingsUrl"].GetStringAsync().Result);

            MtFrontendSettings settings = appSettings.MtFrontend;

            if (!string.IsNullOrEmpty(Configuration["Env"]))
            {
                settings.MarginTradingFront.Env = Configuration["Env"];
            }

            Console.WriteLine($"Env: {settings.MarginTradingFront.Env}");

            SetupLoggers(services, appSettings);

            RegisterDependencies(builder, settings);

            builder.Populate(services);

            ApplicationContainer = builder.Build();

            SetSubscribers(settings);

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            app.UseMiddleware<GlobalErrorHandlerMiddleware>();
            app.UseOptions();

            var settings = ApplicationContainer.Resolve<MtFrontSettings>();
            app.UseCors(builder => builder.WithOrigins(settings.AllowOrigins));

            IWampHost host = ApplicationContainer.Resolve<IWampHost>();
            IWampHostedRealm realm = ApplicationContainer.Resolve<IWampHostedRealm>();
            IDisposable realmMetaService = realm.HostMetaApiService();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "Default", template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSwagger();
            app.UseSwaggerUi();
            app.UseStaticFiles();

            app.Map("/ws", builder =>
            {
                builder.UseWebSockets(new WebSocketOptions {KeepAliveInterval = TimeSpan.FromMinutes(1)});

                var jsonSettings =
                    new JsonSerializerSettings() {Converters = SerializerSettings.GetDefaultConverters()};
                var jsonSerializer = JsonSerializer.Create(jsonSettings);

                host.RegisterTransport(new AspNetCoreWebSocketTransport(builder),
                                       new JTokenJsonBinding(jsonSerializer),
                                       new JTokenMsgpackBinding(jsonSerializer));
            });

            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());

            Application application = app.ApplicationServices.GetService<Application>();

            appLifetime.ApplicationStarted.Register(() =>
            {
                if (!string.IsNullOrEmpty(settings.ApplicationInsightsKey))
                {
                    Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.InstrumentationKey =
                        settings.ApplicationInsightsKey;
                }

                application.StartAsync().Wait();
            });

            appLifetime.ApplicationStopping.Register(() =>
                {
                    realmMetaService.Dispose();
                    application.Stop();
                }
            );

            host.Open();
        }

        private void RegisterDependencies(ContainerBuilder builder, MtFrontendSettings settings)
        {
            var host = new WampHost();
            var realm = host.RealmContainer.GetRealmByName(RealmNames.FrontEnd);

            builder.RegisterInstance(host)
                .As<IWampHost>()
                .SingleInstance();

            builder.RegisterInstance(realm)
                .As<IWampHostedRealm>()
                .SingleInstance();

            builder.RegisterInstance(LogLocator.CommonLog)
                .As<ILog>()
                .SingleInstance();

            builder.Register<IMarginTradingOperationsLogRepository>(ctx =>
                    new MarginTradingOperationsLogRepository(AzureTableStorage<OperationLogEntity>.Create(
                        () => settings.MarginTradingFront.Db.LogsConnString, "MarginTradingFrontendOperationsLog",
                        LogLocator.CommonLog))
                )
                .SingleInstance();

            builder.Register<IClientSettingsRepository>(ctx =>
                AzureRepoFactories.Clients.CreateTraderSettingsRepository(settings.MarginTradingFront.Db.ClientPersonalInfoConnString, LogLocator.CommonLog)
            ).SingleInstance();

            builder.Register<IClientAccountsRepository>(ctx =>
                AzureRepoFactories.Clients.CreateClientsRepository(settings.MarginTradingFront.Db.ClientPersonalInfoConnString, LogLocator.CommonLog)
            ).SingleInstance();

            builder.Register<IAppGlobalSettingsRepositry>(ctx =>
                new AppGlobalSettingsRepository(AzureTableStorage<AppGlobalSettingsEntity>.Create(
                    () => settings.MarginTradingFront.Db.ClientPersonalInfoConnString, "Setup", LogLocator.CommonLog))
            ).SingleInstance();

            builder.Register<IMarginTradingWatchListRepository>(ctx =>
               AzureRepoFactories.MarginTrading.CreateWatchListsRepository(settings.MarginTradingFront.Db.MarginTradingConnString, LogLocator.CommonLog)
           ).SingleInstance();

            builder.RegisterType<WatchListService>()
                .As<IWatchListService>()
                .SingleInstance();

            builder.RegisterType<ClientTokenService>()
                .As<IClientTokenService>()
                .SingleInstance();

            builder.RegisterType<ClientAccountService>()
                .As<IClientAccountService>()
                .SingleInstance();

            builder.RegisterType<MarginTradingOperationsLogService>()
                .As<IMarginTradingOperationsLogService>()
                .SingleInstance();

            var consoleWriter = new ConsoleLWriter(line =>
                {
                    try
                    {
                        if (settings.MarginTradingFront.RemoteConsoleEnabled && !string.IsNullOrEmpty(settings.MarginTradingFront.MetricLoggerLine))
                        {
                            settings.MarginTradingFront.MetricLoggerLine.PostJsonAsync(
                                new
                                {
                                    Id = "Mt-frontend",
                                    Data =
                                    new[]
                                    {
                                        new { Key = "Version", Value = PlatformServices.Default.Application.ApplicationVersion },
                                        new { Key = "Data", Value = line }
                                    }
                                });
                        }
                    }
                    catch { }
                });

            builder.RegisterInstance(consoleWriter)
                .As<IConsole>()
                .SingleInstance();

            builder.RegisterType<RabbitMqHandler>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterInstance(settings)
                .SingleInstance();

            builder.RegisterInstance(settings.MarginTradingFront)
                .SingleInstance();

            builder.RegisterInstance(settings.MarginTradingFront.RequestLoggerSettings)
                .SingleInstance();

            builder.RegisterType<RpcMtFrontend>()
                .As<IRpcMtFrontend>()
                .SingleInstance();

            builder.RegisterType<HttpRequestService>()
                .As<IHttpRequestService>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<MarginTradingSettingsService>()
               .As<IMarginTradingSettingsService>()
               .SingleInstance();

            builder.RegisterType<ThreadSwitcherToNewTask>()
                .As<IThreadSwitcher>()
                .SingleInstance();

            builder.RegisterType<Application>()
                .SingleInstance();

            builder.Register<IClientsSessionsRepository>(ctx =>
                new ClientSessionsClient(settings.MarginTradingFront.SessionServiceApiUrl, LogLocator.CommonLog)
            ).SingleInstance();

            builder.RegisterType<ClientTokenValidator>()
                .As<ISecurityTokenValidator>()
                .SingleInstance();

            builder.RegisterType<WampSessionsService>()
                .AsSelf()
                .SingleInstance();


            builder.RegisterType<RpcFacade>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<MemoryCacheProvider>()
                   .As<ICacheProvider>()
                   .AsSelf()
                   .SingleInstance();

            builder.RegisterType<DateService>()
                .As<IDateService>()
                .SingleInstance();
        }

        private void SetSubscribers(MtFrontendSettings settings)
        {
            MarginTradingBackendServiceLocator.RabbitMqHandler = ApplicationContainer.Resolve<RabbitMqHandler>();
            var log = ApplicationContainer.Resolve<ILog>();
            var consoleWriter = ApplicationContainer.Resolve<IConsole>();

            var pricesSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.MarginTradingLive.MtRabbitMqConnString,
                ExchangeName = settings.MarginTradingFront.RabbitMqQueues.OrderbookPrices.ExchangeName,
                QueueName =
                    QueueHelper.BuildQueueName(settings.MarginTradingFront.RabbitMqQueues.OrderbookPrices.ExchangeName,
                        settings.MarginTradingFront.Env),
                IsDurable = false
            };

            MarginTradingBackendServiceLocator.SubscriberPrices =
                new RabbitMqSubscriber<InstrumentBidAskPair>(pricesSettings,
                        new DefaultErrorHandlingStrategy(log, pricesSettings))
                    .SetMessageDeserializer(new FrontEndDeserializer<InstrumentBidAskPair>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetLogger(log)
                    .SetConsole(consoleWriter)
                    .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessPrices)
                    .Start();

            var accChangeDemoSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.MarginTradingDemo.MtRabbitMqConnString,
                ExchangeName = settings.MarginTradingFront.RabbitMqQueues.AccountChanged.ExchangeName,
                QueueName =
                    QueueHelper.BuildQueueName(settings.MarginTradingFront.RabbitMqQueues.AccountChanged.ExchangeName,
                        settings.MarginTradingFront.Env),
                IsDurable = false
            };

            MarginTradingBackendServiceLocator.SubscriberAccountChangedDemo =
                new RabbitMqSubscriber<AccountChangedMessage>(accChangeDemoSettings,
                        new ResilientErrorHandlingStrategy(log, accChangeDemoSettings, _subscriberRetryTimeout))
                    .SetMessageDeserializer(new FrontEndDeserializer<AccountChangedMessage>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetLogger(log)
                    .SetConsole(consoleWriter)
                    .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessAccountChanged)
                    .Start();

            var accChangedLiveSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.MarginTradingLive.MtRabbitMqConnString,
                ExchangeName = settings.MarginTradingFront.RabbitMqQueues.AccountChanged.ExchangeName,
                QueueName =
                    QueueHelper.BuildQueueName(settings.MarginTradingFront.RabbitMqQueues.AccountChanged.ExchangeName,
                        settings.MarginTradingFront.Env),
                IsDurable = false
            };

            MarginTradingBackendServiceLocator.SubscriberAccountChangedLive =
                new RabbitMqSubscriber<AccountChangedMessage>(accChangedLiveSettings,
                        new ResilientErrorHandlingStrategy(log, accChangedLiveSettings, _subscriberRetryTimeout))
                    .SetMessageDeserializer(new FrontEndDeserializer<AccountChangedMessage>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetLogger(log)
                    .SetConsole(consoleWriter)
                    .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessAccountChanged)
                    .Start();

            var orderDemoSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.MarginTradingDemo.MtRabbitMqConnString,
                ExchangeName = settings.MarginTradingFront.RabbitMqQueues.OrderChanged.ExchangeName,
                QueueName =
                    QueueHelper.BuildQueueName(settings.MarginTradingFront.RabbitMqQueues.OrderChanged.ExchangeName,
                        settings.MarginTradingFront.Env),
                IsDurable = false
            };

            MarginTradingBackendServiceLocator.SubscriberOrderChangedDemo =
                new RabbitMqSubscriber<OrderContract>(orderDemoSettings,
                        new ResilientErrorHandlingStrategy(log, orderDemoSettings, _subscriberRetryTimeout))
                    .SetMessageDeserializer(new FrontEndDeserializer<OrderContract>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetLogger(log)
                    .SetConsole(consoleWriter)
                    .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessOrderChanged)
                    .Start();

            var ordersLiveSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.MarginTradingLive.MtRabbitMqConnString,
                ExchangeName = settings.MarginTradingFront.RabbitMqQueues.OrderChanged.ExchangeName,
                QueueName =
                    QueueHelper.BuildQueueName(settings.MarginTradingFront.RabbitMqQueues.OrderChanged.ExchangeName,
                        settings.MarginTradingFront.Env),
                IsDurable = false
            };

            MarginTradingBackendServiceLocator.SubscriberOrderChangedLive =
                new RabbitMqSubscriber<OrderContract>(ordersLiveSettings,
                        new ResilientErrorHandlingStrategy(log, ordersLiveSettings, _subscriberRetryTimeout))
                    .SetMessageDeserializer(new FrontEndDeserializer<OrderContract>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetLogger(log)
                    .SetConsole(consoleWriter)
                    .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessOrderChanged)
                    .Start();

            var stopoutDemoSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.MarginTradingDemo.MtRabbitMqConnString,
                ExchangeName = settings.MarginTradingFront.RabbitMqQueues.AccountStopout.ExchangeName,
                QueueName =
                    QueueHelper.BuildQueueName(settings.MarginTradingFront.RabbitMqQueues.AccountStopout.ExchangeName,
                        settings.MarginTradingFront.Env),
                IsDurable = false
            };

            MarginTradingBackendServiceLocator.SubscriberAccountStopoutDemo =
                new RabbitMqSubscriber<AccountStopoutBackendContract>(stopoutDemoSettings,
                        new ResilientErrorHandlingStrategy(log, stopoutDemoSettings, _subscriberRetryTimeout))
                    .SetMessageDeserializer(new FrontEndDeserializer<AccountStopoutBackendContract>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetLogger(log)
                    .SetConsole(consoleWriter)
                    .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessAccountStopout)
                    .Start();

            var stopoutLiveSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.MarginTradingLive.MtRabbitMqConnString,
                ExchangeName = settings.MarginTradingFront.RabbitMqQueues.AccountStopout.ExchangeName,
                QueueName =
                    QueueHelper.BuildQueueName(settings.MarginTradingFront.RabbitMqQueues.AccountStopout.ExchangeName,
                        settings.MarginTradingFront.Env),
                IsDurable = false
            };

            MarginTradingBackendServiceLocator.SubscriberAccountStopoutLive =
                new RabbitMqSubscriber<AccountStopoutBackendContract>(stopoutLiveSettings,
                        new ResilientErrorHandlingStrategy(log, stopoutLiveSettings, _subscriberRetryTimeout))
                    .SetMessageDeserializer(new FrontEndDeserializer<AccountStopoutBackendContract>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetLogger(log)
                    .SetConsole(consoleWriter)
                    .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessAccountStopout)
                    .Start();

            var userDemoSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.MarginTradingDemo.MtRabbitMqConnString,
                ExchangeName = settings.MarginTradingFront.RabbitMqQueues.UserUpdates.ExchangeName,
                QueueName =
                    QueueHelper.BuildQueueName(settings.MarginTradingFront.RabbitMqQueues.UserUpdates.ExchangeName,
                        settings.MarginTradingFront.Env),
                IsDurable = false
            };

            MarginTradingBackendServiceLocator.SubscribeUserUpdatesDemo =
                new RabbitMqSubscriber<UserUpdateEntityBackendContract>(userDemoSettings,
                        new ResilientErrorHandlingStrategy(log, userDemoSettings, _subscriberRetryTimeout))
                    .SetMessageDeserializer(new FrontEndDeserializer<UserUpdateEntityBackendContract>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetLogger(log)
                    .SetConsole(consoleWriter)
                    .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessUserUpdates)
                    .Start();

            var userLiveSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.MarginTradingLive.MtRabbitMqConnString,
                ExchangeName = settings.MarginTradingFront.RabbitMqQueues.UserUpdates.ExchangeName,
                QueueName =
                    QueueHelper.BuildQueueName(settings.MarginTradingFront.RabbitMqQueues.UserUpdates.ExchangeName,
                        settings.MarginTradingFront.Env),
                IsDurable = false
            };

            MarginTradingBackendServiceLocator.SubscribeUserUpdatesLive =
                new RabbitMqSubscriber<UserUpdateEntityBackendContract>(userLiveSettings,
                        new ResilientErrorHandlingStrategy(log, userLiveSettings, _subscriberRetryTimeout))
                    .SetMessageDeserializer(new FrontEndDeserializer<UserUpdateEntityBackendContract>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetLogger(log)
                    .SetConsole(consoleWriter)
                    .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessUserUpdates)
                    .Start();
        }

        private static void SetupLoggers(IServiceCollection services, ApplicationSettings settings)
        {
            var consoleLogger = new LogToConsole();

            var comonSlackService =
                services.UseSlackNotificationsSenderViaAzureQueue(settings.SlackNotifications.AzureQueue,
                    consoleLogger);

            var slackService =
                new MtSlackNotificationsSender(comonSlackService, "MT Frontend", settings.MtFrontend.MarginTradingFront.Env);

            var log = services.UseLogToAzureStorage(settings.MtFrontend.MarginTradingFront.Db.LogsConnString,
                slackService, "MarginTradingFrontendLog", consoleLogger);

            var requestsLog = services.UseLogToAzureStorage(settings.MtFrontend.MarginTradingFront.Db.LogsConnString,
                slackService, "MarginTradingFrontendRequestsLog", consoleLogger);

            LogLocator.CommonLog = log;
            LogLocator.RequestsLog = requestsLog;
        }
    }

    public static class MarginTradingBackendServiceLocator
    {
        public static RabbitMqHandler RabbitMqHandler;
        public static RabbitMqSubscriber<InstrumentBidAskPair> SubscriberPrices;
        public static RabbitMqSubscriber<AccountChangedMessage> SubscriberAccountChangedDemo;
        public static RabbitMqSubscriber<AccountChangedMessage> SubscriberAccountChangedLive;
        public static RabbitMqSubscriber<OrderContract> SubscriberOrderChangedDemo;
        public static RabbitMqSubscriber<OrderContract> SubscriberOrderChangedLive;
        public static RabbitMqSubscriber<AccountStopoutBackendContract> SubscriberAccountStopoutDemo;
        public static RabbitMqSubscriber<AccountStopoutBackendContract> SubscriberAccountStopoutLive;
        public static RabbitMqSubscriber<UserUpdateEntityBackendContract> SubscribeUserUpdatesDemo;
        public static RabbitMqSubscriber<UserUpdateEntityBackendContract> SubscribeUserUpdatesLive;
    }
}
