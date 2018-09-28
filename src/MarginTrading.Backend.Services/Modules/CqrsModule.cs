using System.Collections.Generic;
using System.Linq;
using Autofac;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Cqrs.Configuration.BoundedContext;
using Lykke.Cqrs.Configuration.Routing;
using Lykke.Cqrs.Configuration.Saga;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Workflow;
using MarginTrading.SettingsService.Contracts.AssetPair;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Events;

namespace MarginTrading.Backend.Services.Modules
{
    public class CqrsModule : Module
    {
        private const string EventsRoute = "events";
        private const string CommandsRoute = "commands";
        private readonly CqrsSettings _settings;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly ILog _log;
        private readonly long _defaultRetryDelayMs;

        public CqrsModule(CqrsSettings settings, ILog log, MarginTradingSettings marginTradingSettings)
        {
            _settings = settings;
            _marginTradingSettings = marginTradingSettings;
            _log = log;
            _defaultRetryDelayMs = (long) _settings.RetryDelay.TotalMilliseconds;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.ContextNames).AsSelf().SingleInstance();
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>()
                .SingleInstance();
            builder.RegisterType<CqrsSender>().As<ICqrsSender>()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();
            builder.RegisterInstance(new CqrsContextNamesSettings()).AsSelf().SingleInstance();

            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.ConnectionString
            };
            var messagingEngine = new MessagingEngine(_log, new TransportResolver(
                new Dictionary<string, TransportInfo>
                {
                    {
                        "RabbitMq",
                        new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName,
                            rabbitMqSettings.Password, "None", "RabbitMq")
                    }
                }), new RabbitMqTransportFactory());

            // Sagas & command handlers
            builder.RegisterAssemblyTypes(GetType().Assembly).Where(t => 
                new [] {"Saga", "CommandsHandler", "Projection"}.Any(ending=> t.Name.EndsWith(ending))).AsSelf();

            builder.Register(ctx => CreateEngine(ctx, messagingEngine)).As<ICqrsEngine>().SingleInstance()
                .AutoActivate();
        }

        private CqrsEngine CreateEngine(IComponentContext ctx, IMessagingEngine messagingEngine)
        {
            var rabbitMqConventionEndpointResolver =
                new RabbitMqConventionEndpointResolver("RabbitMq", "messagepack",
                    environment: _settings.EnvironmentName);

            var registrations = new List<IRegistration>
            {
                Register.DefaultEndpointResolver(rabbitMqConventionEndpointResolver),
                RegisterDefaultRouting(),
                RegisterSpecialLiquidationSaga(),
                RegisterContext(),
            };

            var fakeGavel = RegisterGavelContextIfNeeded();
            if (fakeGavel != null)
                registrations.Add(fakeGavel);

            return new CqrsEngine(_log, ctx.Resolve<IDependencyResolver>(), messagingEngine,
                new DefaultEndpointProvider(), true, registrations.ToArray());
        }

        private IRegistration RegisterGavelContextIfNeeded()
        {
            if (_marginTradingSettings.ExchangeConnector == ExchangeConnectorType.FakeExchangeConnector)
            {
                var contextRegistration = Register.BoundedContext(_settings.ContextNames.Gavel)
                    .FailedCommandRetryDelay(_defaultRetryDelayMs).ProcessingOptions(CommandsRoute).MultiThreaded(8)
                    .QueueCapacity(1024);
                
                contextRegistration
                    .PublishingEvents(
                        typeof(PriceForSpecialLiquidationCalculatedEvent),
                        typeof(SpecialLiquidationOrderExecutedEvent)
                    ).With(EventsRoute);
                
                return contextRegistration;
            }
            else
            {
                return null;
            }
        }

        private IRegistration RegisterContext()
        {
            var contextRegistration = Register.BoundedContext(_settings.ContextNames.TradingEngine)
                .FailedCommandRetryDelay(_defaultRetryDelayMs).ProcessingOptions(CommandsRoute).MultiThreaded(8)
                .QueueCapacity(1024);

            RegisterWithdrawalCommandsHandler(contextRegistration);
            RegisterSpecialLiquidationCommandsHandler(contextRegistration);
            RegisterAccountsProjection(contextRegistration);
            RegisterAssetPairsProjection(contextRegistration);
            
            contextRegistration.PublishingEvents(typeof(PositionClosedEvent)).With(EventsRoute);

            return contextRegistration;
        }

        private void RegisterAssetPairsProjection(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningEvents(
                    typeof(AssetPairChangedEvent))
                .From(_settings.ContextNames.SettingsService)
                .On(EventsRoute)
                .WithProjection(
                    typeof(AssetPairProjection), _settings.ContextNames.SettingsService);
		}
                    
        private PublishingCommandsDescriptor<IDefaultRoutingRegistration> RegisterDefaultRouting()
        {
            return Register.DefaultRouting
                .PublishingCommands(
                    typeof(SuspendAssetPairCommand),
                    typeof(UnsuspendAssetPairCommand)
                )
                .To(_settings.ContextNames.SettingsService)
                .With(CommandsRoute)
                .PublishingCommands(
                    typeof(StartSpecialLiquidationInternalCommand)
                )
                .To(_settings.ContextNames.TradingEngine)
                .With(CommandsRoute);
        }

        private void RegisterAccountsProjection(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningEvents(
                    typeof(AccountChangedEvent))
                .From(_settings.ContextNames.AccountsManagement).On(EventsRoute)
                .WithProjection(
                    typeof(AccountsProjection), _settings.ContextNames.AccountsManagement);
        }

        private void RegisterWithdrawalCommandsHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningCommands(
                    typeof(FreezeAmountForWithdrawalCommand),
                    typeof(UnfreezeMarginOnFailWithdrawalCommand))
                .On(CommandsRoute)
                .WithCommandsHandler<WithdrawalCommandsHandler>()
                .PublishingEvents(
                    typeof(AmountForWithdrawalFrozenEvent),
                    typeof(AmountForWithdrawalFreezeFailedEvent))
                .With(EventsRoute);
        }

        private IRegistration RegisterSpecialLiquidationSaga()
        {
            var sagaRegistration = RegisterSaga<SpecialLiquidationSaga>();

            sagaRegistration

                .PublishingCommands(
                    typeof(GetPriceForSpecialLiquidationCommand),
                    typeof(ExecuteSpecialLiquidationOrderCommand)
                )
                .To(_settings.ContextNames.Gavel)
                .With(CommandsRoute)

                .ListeningEvents(
                    typeof(PriceForSpecialLiquidationCalculatedEvent),
                    typeof(PriceForSpecialLiquidationCalculationFailedEvent),
                    typeof(SpecialLiquidationOrderExecutedEvent),
                    typeof(SpecialLiquidationOrderExecutionFailedEvent)
                )
                .From(_settings.ContextNames.Gavel)
                .On(EventsRoute)

                .PublishingCommands(
                    typeof(FailSpecialLiquidationInternalCommand),
                    typeof(ExecuteSpecialLiquidationOrdersInternalCommand),
                    typeof(GetPriceForSpecialLiquidationTimeoutInternalCommand)
                )
                .To(_settings.ContextNames.TradingEngine)
                .With(CommandsRoute)

                .ListeningEvents(
                    typeof(SpecialLiquidationOrderExecutedEvent),
                    typeof(SpecialLiquidationStartedInternalEvent),
                    typeof(SpecialLiquidationFinishedEvent),
                    typeof(SpecialLiquidationFailedEvent)
                )
                .From(_settings.ContextNames.TradingEngine)
                .On(EventsRoute);
            
            return sagaRegistration;
        }

        private void RegisterSpecialLiquidationCommandsHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningCommands(
                    typeof(StartSpecialLiquidationCommand),
                    typeof(StartSpecialLiquidationInternalCommand),
                    typeof(GetPriceForSpecialLiquidationTimeoutInternalCommand),
                    typeof(FailSpecialLiquidationInternalCommand),
                    typeof(ExecuteSpecialLiquidationOrdersInternalCommand)
                )
                .On(CommandsRoute)
                .WithCommandsHandler<SpecialLiquidationCommandsHandler>()
                .PublishingEvents(
                    typeof(SpecialLiquidationStartedInternalEvent),
                    typeof(SpecialLiquidationFinishedEvent),
                    typeof(SpecialLiquidationFailedEvent)
                )
                .With(EventsRoute);
        }

        private ISagaRegistration RegisterSaga<TSaga>()
        {
            return Register.Saga<TSaga>($"{_settings.ContextNames.TradingEngine}.{typeof(TSaga).Name}");
        }
    }
}