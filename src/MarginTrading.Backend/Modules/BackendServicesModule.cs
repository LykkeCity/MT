using System;
using System.Collections.Generic;
using Autofac;
using Common;
using Common.Log;
using Flurl.Http;
using Lykke.EmailSenderProducer;
using Lykke.EmailSenderProducer.Interfaces;
using Lykke.RabbitMqBroker.Publisher;
using MarginTrading.Backend.Email;
using MarginTrading.Backend.Middleware.Validator;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Wamp;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using MarginTrading.Services;
using MarginTrading.Services.Events;
using MarginTrading.Services.Generated.ClientAccountServiceApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using WampSharp.V2;
using WampSharp.V2.Realm;

namespace MarginTrading.Backend.Modules
{
    public class BackendServicesModule : Module
    {
        private readonly MarginSettings _settings;
        private readonly IHostingEnvironment _environment;

        public BackendServicesModule(MarginSettings settings, IHostingEnvironment environment)
        {
            _settings = settings;
            _environment = environment;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var host = new WampHost();
            var realm = host.RealmContainer.GetRealmByName(RealmNames.BackEnd);

            builder.RegisterInstance(host)
                .As<IWampHost>()
                .SingleInstance();

            builder.RegisterInstance(realm)
                .As<IWampHostedRealm>()
                .SingleInstance();

            builder.RegisterType<ApiKeyValidator>()
                .As<IApiKeyValidator>()
                .SingleInstance();

            builder.RegisterType<EmailService>()
                .As<IEmailService>()
                .SingleInstance();

            builder.RegisterType<OrderBookSaveService>()
                .As<IStartable>()
                .SingleInstance();

            builder.Register<IServiceBusEmailSettings>(ctx =>
                new ServiceBusEmailSettings
                {
                    Key = _settings.EmailServiceBus.Key,
                    QueueName = _settings.EmailServiceBus.QueueName,
                    NamespaceUrl = _settings.EmailServiceBus.NamespaceUrl,
                    PolicyName = _settings.EmailServiceBus.PolicyName
                }
            ).SingleInstance();

            builder.Register<ITemplateGenerator>(ctx =>
                new MustacheTemplateGenerator(_environment, "Email/Templates")
            ).SingleInstance();

            builder.RegisterType<EmailSenderProducer>()
                .As<IEmailSender>()
                .SingleInstance();

            var consoleWriter = _environment.IsProduction()
                ? new ConsoleLWriter(line =>
                {
                    try
                    {
                        if (_settings.RemoteConsoleEnabled && !string.IsNullOrEmpty(_settings.MetricLoggerLine))
                        {
                            _settings.MetricLoggerLine.PostJsonAsync(
                                new
                                {
                                    Id = "Mt-backend",
                                    Data =
                                    new[]
                                    {
                                        new { Key = "Version", Value = PlatformServices.Default.Application.ApplicationVersion },
                                        new { Key = "Data", Value = line }
                                    }
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                })
                : new ConsoleLWriter(Console.WriteLine);

            builder.RegisterInstance(consoleWriter)
                .As<IConsole>()
                .SingleInstance();

            builder.RegisterType<MarginTradingOperationsLogService>()
                .As<IMarginTradingOperationsLogService>()
                .SingleInstance();

            builder.RegisterType<PricesUpdateRabbitMqNotifier>()
                .As<IEventConsumer<BestPriceChangeEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(_settings).SingleInstance();
            builder.RegisterType<Application>()
                .AsSelf()
                .As<IStartable>()
                .SingleInstance();

			builder.Register<IClientAccountService>(ctx =>
				new ClientAccountService(new Uri(_settings.ClientAccountServiceApiUrl))
			).SingleInstance();

			RegisterPublishers(builder, consoleWriter);
        }

        private void RegisterPublishers(ContainerBuilder builder, IConsole consoleWriter)
        {
            var publishers = new List<string>
            {
                _settings.RabbitMqQueues.AccountHistory.RoutingKeyName,
                _settings.RabbitMqQueues.OrderHistory.RoutingKeyName,
                _settings.RabbitMqQueues.OrderRejected.RoutingKeyName,
                _settings.RabbitMqQueues.OrderbookPrices.RoutingKeyName,
                _settings.RabbitMqQueues.OrderChanged.RoutingKeyName,
                _settings.RabbitMqQueues.AccountChanged.RoutingKeyName,
                _settings.RabbitMqQueues.AccountStopout.RoutingKeyName,
                _settings.RabbitMqQueues.UserUpdates.RoutingKeyName,
                _settings.RabbitMqQueues.Transaction.RoutingKeyName,
                _settings.RabbitMqQueues.OrderReport.RoutingKeyName,
				_settings.RabbitMqQueues.ElementaryTransaction.RoutingKeyName,
				_settings.RabbitMqQueues.AggregateValuesAtRisk.RoutingKeyName,
				_settings.RabbitMqQueues.IndividualValuesAtRisk.RoutingKeyName,
				_settings.RabbitMqQueues.PositionUpdates.RoutingKeyName,
				_settings.RabbitMqQueues.ValueAtRiskLimits.RoutingKeyName
            };

            var rabbitMqSettings = new RabbitMqPublisherSettings
            {
                ConnectionString = _settings.MarginTradingRabbitMqSettings.InternalConnectionString,
                ExchangeName = _settings.MarginTradingRabbitMqSettings.ExchangeName
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
