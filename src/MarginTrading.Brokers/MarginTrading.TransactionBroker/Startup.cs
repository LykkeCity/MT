using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Flurl.Http;
using Lykke.Logs;
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
using MarginTrading.Services;
using Microsoft.Extensions.PlatformAbstractions;
using MarginTrading.Services.Events;
using System.Collections.Generic;
using Lykke.RabbitMqBroker.Publisher;
using MarginTrading.Common.RabbitMq;
using Common;

namespace MarginTrading.TransactionBroker
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
                .AddConsole()
                .AddDebug();

            services.AddSingleton(loggerFactory);
            services.AddLogging();
            services.AddSingleton(Configuration);

            var builder = new ContainerBuilder();

            MtBackendSettings mtSettings = Environment.IsDevelopment()
                ? Configuration.Get<MtBackendSettings>()
                : Configuration["SettingsUrl"].GetJsonAsync<MtBackendSettings>().Result;

            bool isLive = Configuration.IsLive();
            MarginSettings settings = isLive ? mtSettings.MtBackend.MarginTradingLive : mtSettings.MtBackend.MarginTradingDemo;
            settings.IsLive = isLive;

            Console.WriteLine($"IsLive: {settings.IsLive}");

			RegisterPublishers(builder, settings);
			RegisterRepositories(builder, settings);
            RegisterServices(builder);

            builder.RegisterInstance(settings).SingleInstance();
            builder.RegisterType<Application>()
                .AsSelf()
                .As<IStartable>()
                .SingleInstance();

            builder.Populate(services);
            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            Application application = app.ApplicationServices.GetService<Application>();

            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());

            appLifetime.ApplicationStarted.Register(() =>
                application.RunAsync().Wait()
            );

            appLifetime.ApplicationStopping.Register(() =>
                {
                    application.Stop();
                    application.StopApplication();
                }
            );
        }

        private void RegisterRepositories(ContainerBuilder builder, MarginSettings settings)
        {
            LykkeLogToAzureStorage log = new LykkeLogToAzureStorage(PlatformServices.Default.Application.ApplicationName,
                new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "MarginTradingTransactionBrokerLog", null));

            builder.RegisterInstance((ILog)log)
                .As<ILog>()
                .SingleInstance();

            builder.Register<IMarginTradingTransactionRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateTransactionRepository(settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingPositionRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreatePositionRepository(settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingOrdersHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateOrdersHistoryRepository(settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IElementaryTransactionsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateElementaryTransactionsRepository(settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingAssetsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAssetsRepository(settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IServiceMonitoringRepository>(ctx =>
                AzureRepoFactories.Monitoring.CreateServiceMonitoringRepository(settings.Db.SharedStorageConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingAccountAssetRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountAssetsRepository(settings.Db.MarginTradingConnString, log)
            ).SingleInstance();
        }

        private void RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterType<QuoteCacheService>()
                .As<IQuoteCacheService>()
                .SingleInstance();

            builder.RegisterType<InstrumentsCache>()
                .As<IInstrumentsCache>()
                .SingleInstance();

            builder.RegisterType<TransactionService>()
                .As<ITransactionService>()
                .SingleInstance();

            builder.RegisterType<ElementaryTransactionService>()
                .As<IElementaryTransactionService>()
                .SingleInstance();

			builder.RegisterType<AccountsCacheService>()
				.As<IAccountsCacheService>()
				.SingleInstance();

			builder.RegisterType<EventChannel<PositionUpdateEventArgs>>()
				.As<IEventChannel<PositionUpdateEventArgs>>()
				.SingleInstance();

			builder.RegisterType<RabbitMqNotifyService>()
				.As<IRabbitMqNotifyService>()
				.SingleInstance();
		}

		private void RegisterPublishers(ContainerBuilder builder, MarginSettings settings)
		{
			var consoleWriter = new ConsoleLWriter(Console.WriteLine);

			builder.RegisterInstance(consoleWriter)
				.As<IConsole>()
				.SingleInstance();

			var publishers = new List<string>
			{
				settings.RabbitMqQueues.ElementaryTransaction.RoutingKeyName,
			};

			var rabbitMqSettings = new RabbitMqPublisherSettings
			{
				ConnectionString = settings.MarginTradingRabbitMqSettings.InternalConnectionString,
				ExchangeName = settings.MarginTradingRabbitMqSettings.ExchangeName
			};

			var bytesSerializer = new BytesStringSerializer();

			foreach (string routingKey in publishers)
			{
				var pub = new RabbitMqPublisher<string>(rabbitMqSettings)
					.SetSerializer(bytesSerializer)
					.SetPublishStrategy(new TopicPublishStrategy(routingKey))
					.SetConsole(consoleWriter)
					.Start();

				builder.RegisterInstance(pub)
					.Named<IMessageProducer<string>>(routingKey)
					.As<IStopable>()
					.SingleInstance();
			}
		}
	}
}