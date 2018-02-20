using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.AzureQueueIntegration;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Json;
using MarginTrading.Common.Modules;
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
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

#pragma warning disable 1591

namespace MarginTrading.Frontend
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

            RegisterModules(builder, appSettings);

            builder.Populate(services);

            ApplicationContainer = builder.Build();

            SetSubscribers(settings.CurrentValue);

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            app.UseGlobalErrorHandler();
            app.UseOptions();

            var settings = ApplicationContainer.Resolve<MtFrontSettings>();

            if (settings.CorsSettings.Enabled)
            {
                app.UseCors(builder =>
                {
                    builder.WithOrigins(settings.CorsSettings.AllowOrigins)
                        .WithHeaders(settings.CorsSettings.AllowHeaders)
                        .WithMethods(settings.CorsSettings.AllowMethods);

                    if (settings.CorsSettings.AllowCredentials)
                        builder.AllowCredentials();
                });
            }
            

            var host = ApplicationContainer.Resolve<IWampHost>();
            var realm = ApplicationContainer.Resolve<IWampHostedRealm>();
            var realmMetaService = realm.HostMetaApiService();

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

            var application = app.ApplicationServices.GetService<Application>();

            appLifetime.ApplicationStarted.Register(() =>
            {
                if (!string.IsNullOrEmpty(settings.ApplicationInsightsKey))
                {
                    Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.InstrumentationKey =
                        settings.ApplicationInsightsKey;
                }

                application.StartAsync().Wait();
                
                LogLocator.CommonLog?.WriteMonitorAsync("", "", settings.Env + " Started");
            });

            appLifetime.ApplicationStopping.Register(() =>
                {
                    LogLocator.CommonLog?.WriteMonitorAsync("", "", settings.Env + " Terminating");
                    realmMetaService.Dispose();
                    application.Stop();
                }
            );

            host.Open();
        }

        private void RegisterModules(ContainerBuilder builder, IReloadingManager<ApplicationSettings> appSettings)
        {
            var settings = appSettings.Nested(s => s.MtFrontend);
            
            builder.RegisterModule(new FrontendModule(settings));
            builder.RegisterModule(new MarginTradingCommonModule());
            builder.RegisterModule(new FrontendExternalServicesModule(appSettings));
        }

        private void SetSubscribers(MtFrontendSettings settings)
        {
            var rabbitMqService = ApplicationContainer.Resolve<IRabbitMqService>();
            var rabbitMqHandler = ApplicationContainer.Resolve<RabbitMqHandler>();

            // Best prices (only live)

            Subscribe<BidAskPairRabbitMqContract>(rabbitMqService, settings.MarginTradingLive.MtRabbitMqConnString,
                settings.MarginTradingFront.RabbitMqQueues.OrderbookPrices.ExchangeName,
                settings.MarginTradingFront.Env, rabbitMqHandler.ProcessPrices);

            // Account changes

            Subscribe<AccountChangedMessage>(rabbitMqService, settings.MarginTradingLive.MtRabbitMqConnString,
                settings.MarginTradingFront.RabbitMqQueues.AccountChanged.ExchangeName,
                settings.MarginTradingFront.Env, rabbitMqHandler.ProcessAccountChanged);

            Subscribe<AccountChangedMessage>(rabbitMqService, settings.MarginTradingDemo.MtRabbitMqConnString,
                settings.MarginTradingFront.RabbitMqQueues.AccountChanged.ExchangeName,
                settings.MarginTradingFront.Env, rabbitMqHandler.ProcessAccountChanged);

            // Order changes

            Subscribe<OrderContract>(rabbitMqService, settings.MarginTradingLive.MtRabbitMqConnString,
                settings.MarginTradingFront.RabbitMqQueues.OrderChanged.ExchangeName,
                settings.MarginTradingFront.Env, rabbitMqHandler.ProcessOrderChanged);

            Subscribe<OrderContract>(rabbitMqService, settings.MarginTradingDemo.MtRabbitMqConnString,
                settings.MarginTradingFront.RabbitMqQueues.OrderChanged.ExchangeName,
                settings.MarginTradingFront.Env, rabbitMqHandler.ProcessOrderChanged);

            // Stopout

            Subscribe<AccountStopoutBackendContract>(rabbitMqService, settings.MarginTradingLive.MtRabbitMqConnString,
                settings.MarginTradingFront.RabbitMqQueues.AccountStopout.ExchangeName,
                settings.MarginTradingFront.Env, rabbitMqHandler.ProcessAccountStopout);

            Subscribe<AccountStopoutBackendContract>(rabbitMqService, settings.MarginTradingDemo.MtRabbitMqConnString,
                settings.MarginTradingFront.RabbitMqQueues.AccountStopout.ExchangeName,
                settings.MarginTradingFront.Env, rabbitMqHandler.ProcessAccountStopout);

            // User updates

            Subscribe<UserUpdateEntityBackendContract>(rabbitMqService, settings.MarginTradingLive.MtRabbitMqConnString,
                settings.MarginTradingFront.RabbitMqQueues.UserUpdates.ExchangeName,
                settings.MarginTradingFront.Env, rabbitMqHandler.ProcessUserUpdates);

            Subscribe<UserUpdateEntityBackendContract>(rabbitMqService, settings.MarginTradingDemo.MtRabbitMqConnString,
                settings.MarginTradingFront.RabbitMqQueues.UserUpdates.ExchangeName,
                settings.MarginTradingFront.Env, rabbitMqHandler.ProcessUserUpdates);
            
            // Trades
            
            Subscribe<TradeContract>(rabbitMqService, settings.MarginTradingLive.MtRabbitMqConnString,
                settings.MarginTradingFront.RabbitMqQueues.Trades.ExchangeName,
                settings.MarginTradingFront.Env, rabbitMqHandler.ProcessTrades);

            Subscribe<TradeContract>(rabbitMqService, settings.MarginTradingDemo.MtRabbitMqConnString,
                settings.MarginTradingFront.RabbitMqQueues.Trades.ExchangeName,
                settings.MarginTradingFront.Env, rabbitMqHandler.ProcessTrades);
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

        private void Subscribe<TMessage>(IRabbitMqService rabbitMqService, string connectionString,
            string exchangeName, string env, Func<TMessage, Task> handler)
        {
            var settings = new RabbitMqSettings
            {
                ConnectionString = connectionString,
                ExchangeName = exchangeName,
                IsDurable = false
            };

            rabbitMqService.Subscribe(settings, env, handler);
        }
    }
}
