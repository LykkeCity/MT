using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Flurl.Http;
using Lykke.AzureQueueIntegration;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Json;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Contract.ClientContracts;
using MarginTrading.Contract.RabbitMqMessageModels;
using MarginTrading.Frontend.Infrastructure;
using MarginTrading.Frontend.Middleware;
using MarginTrading.Frontend.Modules;
using MarginTrading.Frontend.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
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
                .AddDevJson(env)
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


            var appSettings = Configuration.LoadSettings<ApplicationSettings>()
                .Nested(s =>
                {
                    if (!string.IsNullOrEmpty(Configuration["Env"]))
                    {
                        s.MtFrontend.MarginTradingFront.Env = Configuration["Env"];
                    }

                    return s;
                });

            var settings = appSettings.Nested(s => s.MtFrontend);


            Console.WriteLine($"Env: {settings.CurrentValue.MarginTradingFront.Env}");

            SetupLoggers(services, appSettings);

            RegisterModules(builder, settings);

            builder.Populate(services);

            ApplicationContainer = builder.Build();

            SetSubscribers(settings.CurrentValue);

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
                
                LogLocator.CommonLog?.WriteMonitorAsync("", "", "Started");
            });

            appLifetime.ApplicationStopping.Register(() =>
                {
                    LogLocator.CommonLog?.WriteMonitorAsync("", "", "Terminating");
                    realmMetaService.Dispose();
                    application.Stop();
                }
            );

            host.Open();
        }

        private void RegisterModules(ContainerBuilder builder, IReloadingManager<MtFrontendSettings> settings)
        {
            builder.RegisterModule(new FrontendModule(settings));

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
                new RabbitMqSubscriber<BidAskPairRabbitMqContract>(pricesSettings,
                        new DefaultErrorHandlingStrategy(log, pricesSettings))
                    .SetMessageDeserializer(new FrontEndDeserializer<BidAskPairRabbitMqContract>())
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

        private static void SetupLoggers(IServiceCollection services, IReloadingManager<ApplicationSettings> settings)
        {
            var consoleLogger = new LogToConsole();

            var azureQueue = new AzureQueueSettings
            {
                ConnectionString = settings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                QueueName = settings.CurrentValue.SlackNotifications.AzureQueue.QueueName
            };

            var comonSlackService =
                services.UseSlackNotificationsSenderViaAzureQueue(azureQueue, consoleLogger);

            var slackService =
                new MtSlackNotificationsSender(comonSlackService, "MT Frontend", settings.CurrentValue.MtFrontend.MarginTradingFront.Env);

            // Order of logs registration is important - UseLogToAzureStorage() registers ILog in container.
            // Last registration wins.
            LogLocator.RequestsLog = services.UseLogToAzureStorage(settings.Nested(s => s.MtFrontend.MarginTradingFront.Db.LogsConnString),
                slackService, "MarginTradingFrontendRequestsLog", consoleLogger);

            LogLocator.CommonLog = services.UseLogToAzureStorage(settings.Nested(s => s.MtFrontend.MarginTradingFront.Db.LogsConnString),
                slackService, "MarginTradingFrontendLog", consoleLogger);
        }
    }

    public static class MarginTradingBackendServiceLocator
    {
        public static RabbitMqHandler RabbitMqHandler;
        public static RabbitMqSubscriber<BidAskPairRabbitMqContract> SubscriberPrices;
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
