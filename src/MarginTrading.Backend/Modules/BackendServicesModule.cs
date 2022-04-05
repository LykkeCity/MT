// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Autofac;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Strategies;
using Lykke.Snow.Common.Correlation.RabbitMq;
using MarginTrading.Backend.Email;
using MarginTrading.Backend.Middleware.Validator;
using MarginTrading.Common.RabbitMq;
using Microsoft.AspNetCore.Hosting;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.EventsConsumers;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Quotes;
using MarginTrading.Backend.Services.RabbitMq;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.Settings;
using MarginTrading.Common.Services;
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Modules
{
    public class BackendServicesModule : Module
    {
        private readonly MtBackendSettings _mtSettings;
        private readonly MarginTradingSettings _settings;
        private readonly IHostingEnvironment _environment;
        private readonly ILog _log;

        public BackendServicesModule(
            MtBackendSettings mtSettings,
            MarginTradingSettings settings,
            IHostingEnvironment environment,
            ILog log)
        {
            _mtSettings = mtSettings;
            _settings = settings;
            _environment = environment;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ApiKeyValidator>()
                .As<IApiKeyValidator>()
                .SingleInstance();

            builder.RegisterType<EmailService>()
                .As<IEmailService>()
                .SingleInstance();

            builder.RegisterType<OrderBookSaveService>()
                .AsSelf()
                .SingleInstance();

            builder.Register<ITemplateGenerator>(ctx =>
                new MustacheTemplateGenerator(_environment, Path.Combine("Email","Templates"))
            ).SingleInstance();

            var consoleWriter = new ConsoleLWriter(Console.WriteLine);

            builder.RegisterInstance(consoleWriter)
                .As<IConsole>()
                .SingleInstance();

            if (_settings.WriteOperationLog && _settings.UseSerilog)
            {
                _log.WriteWarning(nameof(BackendServicesModule), nameof(Load), 
                    $"Operations log will not be written, because {nameof(_settings.UseSerilog)} is enabled.");
            }
            
            builder.RegisterType<OperationsLogService>()
                .As<IOperationsLogService>()
                .WithParameter(new TypedParameter(typeof(bool), _settings.WriteOperationLog && !_settings.UseSerilog))
                .SingleInstance();

            builder.RegisterType<PricesUpdateRabbitMqNotifier>()
                .As<IEventConsumer<BestPriceChangeEventArgs>>()
                .SingleInstance();

            builder.RegisterType<Application>()
                .AsSelf()
                .SingleInstance()
                .OnActivated(args => args.Instance.StartApplicationAsync().Wait());

            builder.RegisterType<BackendMaintenanceModeService>()
                .As<IMaintenanceModeService>()
                .SingleInstance();

            builder.RegisterType<UpdatedAccountsStatsConsumer>()
                .As<IEventConsumer<AccountBalanceChangedEventArgs>>()
                .As<IEventConsumer<OrderPlacedEventArgs>>()
                .As<IEventConsumer<OrderExecutedEventArgs>>()
                .As<IEventConsumer<OrderCancelledEventArgs>>()
                .SingleInstance();

            builder.RegisterType<MigrationService>()
                .As<IMigrationService>()
                .SingleInstance();

            builder.RegisterType<EquivalentPricesService>()
                .As<IEquivalentPricesService>()
                .SingleInstance();

            builder.RegisterType<ManualRfqService>()
                .As<IRfqService>()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();

            builder.RegisterType<QueueValidationService>()
                .As<IQueueValidationService>()
                .WithParameter("connectionString", _mtSettings.MtBackend.StartupQueuesChecker.ConnectionString)
                .WithParameter("queueNames", new List<string>
                {
                    _mtSettings.MtBackend.StartupQueuesChecker.OrderHistoryQueueName,
                    _mtSettings.MtBackend.StartupQueuesChecker.PositionHistoryQueueName
                })
                .SingleInstance();

            builder.RegisterChaosKitty(_settings.ChaosKitty);
            
            builder.RegisterType<PublishingQueueRepository>()
                .As<IPublishingQueueRepository>()
                .SingleInstance();

            builder.RegisterType<FakeSnapshotService>()
                .As<IFakeSnapshotService>()
                .SingleInstance();

            builder.RegisterType<RfqPauseService>()
                .As<IRfqPauseService>()
                .SingleInstance();

            RegisterPublishers(builder, consoleWriter);
        }

        private void RegisterPublishers(ContainerBuilder builder, IConsole consoleWriter)
        {
            var publishers = new List<string>
            {
                _settings.RabbitMqQueues.OrderHistory.ExchangeName,
                _settings.RabbitMqQueues.OrderbookPrices.ExchangeName,
                _settings.RabbitMqQueues.AccountMarginEvents.ExchangeName,
                _settings.RabbitMqQueues.AccountStats.ExchangeName,
                _settings.RabbitMqQueues.Trades.ExchangeName,
                _settings.RabbitMqQueues.PositionHistory.ExchangeName,
                _settings.RabbitMqQueues.ExternalOrder.ExchangeName,
            };

            var bytesSerializer = new BytesStringSerializer();

            foreach (var exchangeName in publishers)
            {
                builder
                    .Register(ctx =>
                    {
                        var pub = new RabbitMqPublisher<string>(ctx.Resolve<ILoggerFactory>(), new RabbitMqSubscriptionSettings
                            {
                                ConnectionString = _settings.MtRabbitMqConnString,
                                ExchangeName = exchangeName
                            })
                            .SetSerializer(bytesSerializer)
                            .SetPublishStrategy(new DefaultFanoutPublishStrategy(new RabbitMqSubscriptionSettings {IsDurable = true}))
                            .DisableInMemoryQueuePersistence()
                            .SetWriteHeadersFunc(ctx.Resolve<RabbitMqCorrelationManager>().BuildCorrelationHeadersIfExists);
                        pub.Start();
                        return pub;
                    })
                    .Named<IMessageProducer<string>>(exchangeName)
                    .As<IStartStop>()
                    .SingleInstance();
            }
        }
    }
}
