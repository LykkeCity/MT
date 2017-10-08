using System;
using System.Collections.Generic;
using System.IO;
using Autofac;
using Common;
using Common.Log;
using Flurl.Http;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Backend.Email;
using MarginTrading.Backend.Middleware.Validator;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Wamp;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using MarginTrading.Services;
using MarginTrading.Services.Events;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Lykke.Service.EmailSender;
using MarginTrading.Services.Infrastructure;
using MarginTrading.Services.Settings;

namespace MarginTrading.Backend.Modules
{
    public class BackendServicesModule : Module
    {
        private readonly MtBackendSettings _mtSettings;
        private readonly MarginSettings _settings;
        private readonly IHostingEnvironment _environment;
        private readonly ILog _log;

        public BackendServicesModule(MtBackendSettings mtSettings, MarginSettings settings, IHostingEnvironment environment, ILog log)
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
                .SingleInstance()
                .OnActivated(args => args.Instance.Start());

            builder.Register<ITemplateGenerator>(ctx =>
                new MustacheTemplateGenerator(_environment, Path.Combine("Email","Templates"))
            ).SingleInstance();

            builder.Register<IEmailSender>(ctx =>
                new EmailSenderClient(_mtSettings.EmailSender.ServiceUrl, _log)
            ).SingleInstance();

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

            builder.RegisterType<Application>()
                .AsSelf()
                .SingleInstance()
                .OnActivated(args => args.Instance.Start());

            builder.RegisterType<ClientAccountService>()
                .As<IClientAccountService>()
                .SingleInstance();

            builder.RegisterType<BackendMaintenanceModeService>()
                .As<IMaintenanceModeService>()
                .SingleInstance();

            builder.RegisterType<UpdatedAccountsTrackingService>()
                .As<IUpdatedAccountsTrackingService>()
                .As<IEventConsumer<AccountBalanceChangedEventArgs>>()
                .SingleInstance();

            RegisterPublishers(builder, consoleWriter);
        }

        private void RegisterPublishers(ContainerBuilder builder, IConsole consoleWriter)
        {
            var publishers = new List<string>
            {
                _settings.RabbitMqQueues.AccountHistory.ExchangeName,
                _settings.RabbitMqQueues.OrderHistory.ExchangeName,
                _settings.RabbitMqQueues.OrderRejected.ExchangeName,
                _settings.RabbitMqQueues.OrderbookPrices.ExchangeName,
                _settings.RabbitMqQueues.OrderChanged.ExchangeName,
                _settings.RabbitMqQueues.AccountChanged.ExchangeName,
                _settings.RabbitMqQueues.AccountStopout.ExchangeName,
                _settings.RabbitMqQueues.UserUpdates.ExchangeName,
                _settings.RabbitMqQueues.AccountMarginEvents.ExchangeName
            };

            var bytesSerializer = new BytesStringSerializer();

            foreach (string exchangeName in publishers)
            {
                var pub = new RabbitMqPublisher<string>(new RabbitMqSubscriptionSettings
                    {
                        ConnectionString = _settings.MtRabbitMqConnString,
                        ExchangeName = exchangeName
                    })
                    .SetSerializer(bytesSerializer)
                    .SetPublishStrategy(new DefaultFanoutPublishStrategy(new RabbitMqSubscriptionSettings {IsDurable = true}))
                    .SetLogger(_log)
                    .SetConsole(consoleWriter)
                    .Start();

                builder.RegisterInstance(pub)
                    .Named<IMessageProducer<string>>(exchangeName)
                    .As<IStopable>()
                    .SingleInstance();
            }
        }
    }
}
