// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using BookKeeper.Client.Workflow.Commands;
using BookKeeper.Client.Workflow.Events;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Cqrs.Configuration.BoundedContext;
using Lykke.Cqrs.Configuration.Routing;
using Lykke.Cqrs.Configuration.Saga;
using Lykke.Cqrs.Middleware.Logging;
using Lykke.MarginTrading.OrderBookService.Contracts.Models;
using Lykke.Messaging.Serialization;
using Lykke.Snow.Common.Correlation.Cqrs;
using Lykke.Snow.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Contracts.Workflow.Liquidation.Events;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Workflow;
using MarginTrading.Backend.Services.Workflow.Liquidation;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.Liquidation.Events;
using MarginTrading.AssetService.Contracts.AssetPair;
using MarginTrading.AssetService.Contracts.ClientProfiles;
using MarginTrading.AssetService.Contracts.ClientProfileSettings;
using MarginTrading.AssetService.Contracts.MarketSettings;
using MarginTrading.AssetService.Contracts.Products;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Events;
using Common.Log;

namespace MarginTrading.Backend.Services.Modules
{
    public class CqrsModule : Module
    {
        private const string EventsRoute = "events";
        private const string AccountProjectionRoute = "a";
        private const string CommandsRoute = "commands";
        private readonly CqrsSettings _settings;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly long _defaultRetryDelayMs;
        private readonly ILog _log;

        public CqrsModule(CqrsSettings settings,
            MarginTradingSettings marginTradingSettings,
            ILog log)
        {
            _settings = settings;
            _marginTradingSettings = marginTradingSettings;
            _defaultRetryDelayMs = (long)_settings.RetryDelay.TotalMilliseconds;
            _log = log;
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

            // Sagas & command handlers
            builder.RegisterAssemblyTypes(GetType().Assembly).Where(t =>
                new[] {"Saga", "CommandsHandler", "Projection"}.Any(ending => t.Name.EndsWith(ending))).AsSelf();

            builder.Register(CreateEngine)
                .As<ICqrsEngine>()
                .SingleInstance();
        }

        private CqrsEngine CreateEngine(IComponentContext ctx)
        {
            var rabbitMqConventionEndpointResolver =
                new RabbitMqConventionEndpointResolver("RabbitMq", SerializationFormat.MessagePack,
                    environment: _settings.EnvironmentName);

            var registrations = new List<IRegistration>
            {
                Register.DefaultEndpointResolver(rabbitMqConventionEndpointResolver),
                RegisterDefaultRouting(),
                RegisterSpecialLiquidationSaga(),
                RegisterLiquidationSaga(),
                RegisterContext(),
                Register.CommandInterceptors(new DefaultCommandLoggingInterceptor(_log)),
                Register.EventInterceptors(new DefaultEventLoggingInterceptor(_log))
            };

            var fakeGavel = RegisterGavelContextIfNeeded();
            if (fakeGavel != null)
                registrations.Add(fakeGavel);

            var correlationManager = ctx.Resolve<CqrsCorrelationManager>();
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = new Uri(_settings.ConnectionString, UriKind.Absolute)
            };
            var engine = new RabbitMqCqrsEngine(_log,
                ctx.Resolve<IDependencyResolver>(),
                new DefaultEndpointProvider(),
                rabbitMqSettings.Endpoint.ToString(),
                rabbitMqSettings.UserName,
                rabbitMqSettings.Password,
                true,
                registrations.ToArray());
            engine.SetReadHeadersAction(correlationManager.FetchCorrelationIfExists);
            engine.SetWriteHeadersFunc(correlationManager.BuildCorrelationHeadersIfExists);

            return engine;
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
                        typeof(PriceForSpecialLiquidationCalculationFailedEvent),
                        typeof(OrderExecutionOrderBookContract)
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
            RegisterDeleteAccountsCommandsHandler(contextRegistration);
            RegisterSpecialLiquidationCommandsHandler(contextRegistration);
            RegisterLiquidationCommandsHandler(contextRegistration);
            RegisterEodCommandsHandler(contextRegistration);
            RegisterAccountsProjection(contextRegistration);
            RegisterProductChangedProjection(contextRegistration);
            RegisterClientProfileChangedProjection(contextRegistration);
            RegisterClientProfileSettingsChangedProjection(contextRegistration);
            RegisterMarketSettingsChangedProjection(contextRegistration);
            RegisterClientProfileSettingsProjection(contextRegistration);
            RegisterMarketStateChangedProjection(contextRegistration);

            contextRegistration.PublishingEvents(typeof(PositionClosedEvent)).With(EventsRoute);
            contextRegistration.PublishingEvents(typeof(CompiledScheduleChangedEvent)).With(EventsRoute);
            contextRegistration.PublishingEvents(typeof(MarketStateChangedEvent)).With(EventsRoute);
            contextRegistration.PublishingEvents(typeof(OvernightMarginParameterChangedEvent)).With(EventsRoute);
            contextRegistration.PublishingEvents(typeof(OrderPlacementRejectedEvent)).With(EventsRoute);
            contextRegistration.PublishingEvents(typeof(ExpirationProcessStartedEvent)).With(EventsRoute);

            return contextRegistration;
        }

        private void RegisterClientProfileSettingsProjection(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningEvents(
                    typeof(ClientProfileSettingsChangedEvent))
                .From(_settings.ContextNames.SettingsService)
                .On(nameof(ClientProfileSettingsChangedEvent))
                .WithProjection(
                    typeof(ClientProfileSettingsProjection), _settings.ContextNames.SettingsService);
        }

        private void RegisterProductChangedProjection(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningEvents(
                    typeof(ProductChangedEvent))
                .From(_settings.ContextNames.SettingsService)
                .On(nameof(ProductChangedEvent))
                .WithProjection(
                    typeof(ProductChangedProjection), _settings.ContextNames.SettingsService);
        }

        private void RegisterMarketStateChangedProjection(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningEvents(
                    typeof(MarketStateChangedEvent))
                .From(_settings.ContextNames.TradingEngine)
                .On(EventsRoute)
                .WithProjection(
                    typeof(PlatformClosureProjection), _settings.ContextNames.TradingEngine);
        }

        private void RegisterClientProfileChangedProjection(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningEvents(
                    typeof(ClientProfileChangedEvent))
                .From(_settings.ContextNames.SettingsService)
                .On(nameof(ClientProfileChangedEvent))
                .WithProjection(
                    typeof(ClientProfileChangedProjection), _settings.ContextNames.SettingsService);
        }

        private void RegisterClientProfileSettingsChangedProjection(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningEvents(
                    typeof(ClientProfileSettingsChangedEvent))
                .From(_settings.ContextNames.SettingsService)
                .On(nameof(ClientProfileSettingsChangedEvent))
                .WithProjection(
                    typeof(ClientProfileSettingsChangedProjection), _settings.ContextNames.SettingsService);
        }

        private void RegisterMarketSettingsChangedProjection(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningEvents(
                    typeof(MarketSettingsChangedEvent))
                .From(_settings.ContextNames.SettingsService)
                .On(nameof(MarketSettingsChangedEvent))
                .WithProjection(
                    typeof(MarketSettingsChangedProjection), _settings.ContextNames.SettingsService);
        }

        private PublishingCommandsDescriptor<IDefaultRoutingRegistration> RegisterDefaultRouting()
        {
            return Register.DefaultRouting
                .PublishingCommands(typeof(ChangeProductSuspendedStatusCommand))
                .To(_settings.ContextNames.SettingsService)
                .With(CommandsRoute)
                .PublishingCommands(
                    typeof(StartLiquidationInternalCommand),
                    typeof(ResumeLiquidationInternalCommand),
                    typeof(StartSpecialLiquidationInternalCommand),
                    typeof(ResumePausedSpecialLiquidationCommand)
                )
                .To(_settings.ContextNames.TradingEngine)
                .With(CommandsRoute);
        }

        private void RegisterAccountsProjection(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningEvents(
                    typeof(AccountChangedEvent))
                .From(_settings.ContextNames.AccountsManagement).On(AccountProjectionRoute)
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
                    typeof(AmountForWithdrawalFreezeFailedEvent),
                    typeof(UnfreezeMarginOnFailSucceededWithdrawalEvent))
                .With(EventsRoute);
        }

        private void RegisterDeleteAccountsCommandsHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningCommands(
                    typeof(BlockAccountsForDeletionCommand),
                    typeof(MtCoreFinishAccountsDeletionCommand))
                .On(CommandsRoute)
                .WithCommandsHandler<DeleteAccountsCommandsHandler>()
                .PublishingEvents(
                    typeof(AccountsBlockedForDeletionEvent),
                    typeof(MtCoreDeleteAccountsFinishedEvent))
                .With(EventsRoute);
        }

        private IRegistration RegisterSpecialLiquidationSaga()
        {
            var sagaRegistration = RegisterSaga<SpecialLiquidationSaga>();

            sagaRegistration
                .PublishingCommands(
                    typeof(GetPriceForSpecialLiquidationCommand)
                )
                .To(_settings.ContextNames.Gavel)
                .With(CommandsRoute)
                .ListeningEvents(
                    typeof(PriceForSpecialLiquidationCalculatedEvent),
                    typeof(PriceForSpecialLiquidationCalculationFailedEvent)
                )
                .From(_settings.ContextNames.Gavel)
                .On(EventsRoute)
                .PublishingCommands(
                    typeof(FailSpecialLiquidationInternalCommand),
                    typeof(ExecuteSpecialLiquidationOrderCommand),
                    typeof(ExecuteSpecialLiquidationOrdersInternalCommand),
                    typeof(GetPriceForSpecialLiquidationTimeoutInternalCommand),
                    typeof(ResumeLiquidationInternalCommand),
                    typeof(CancelSpecialLiquidationCommand),
                    typeof(ClosePositionsRegularFlowCommand),
                    typeof(ResumePausedSpecialLiquidationCommand)
                )
                .To(_settings.ContextNames.TradingEngine)
                .With(CommandsRoute)
                .ListeningEvents(
                    typeof(SpecialLiquidationOrderExecutedEvent),
                    typeof(SpecialLiquidationStartedInternalEvent),
                    typeof(SpecialLiquidationOrderExecutionFailedEvent),
                    typeof(SpecialLiquidationFinishedEvent),
                    typeof(SpecialLiquidationFailedEvent),
                    typeof(SpecialLiquidationCancelledEvent),
                    typeof(ResumePausedSpecialLiquidationFailedEvent),
                    typeof(ResumePausedSpecialLiquidationSucceededEvent)
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
                    typeof(ExecuteSpecialLiquidationOrderCommand),
                    typeof(FailSpecialLiquidationInternalCommand),
                    typeof(ExecuteSpecialLiquidationOrdersInternalCommand),
                    typeof(CancelSpecialLiquidationCommand),
                    typeof(ClosePositionsRegularFlowCommand),
                    typeof(ResumePausedSpecialLiquidationCommand)
                )
                .On(CommandsRoute)
                .WithCommandsHandler<SpecialLiquidationCommandsHandler>()
                .PublishingEvents(
                    typeof(SpecialLiquidationStartedInternalEvent),
                    typeof(SpecialLiquidationOrderExecutedEvent),
                    typeof(SpecialLiquidationOrderExecutionFailedEvent),
                    typeof(SpecialLiquidationFinishedEvent),
                    typeof(SpecialLiquidationFailedEvent),
                    typeof(SpecialLiquidationCancelledEvent),
                    typeof(ResumePausedSpecialLiquidationFailedEvent),
                    typeof(ResumePausedSpecialLiquidationSucceededEvent)
                )
                .With(EventsRoute);
        }

        private void RegisterEodCommandsHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningCommands(
                    typeof(CreateSnapshotCommand)
                )
                .On(CommandsRoute)
                .WithCommandsHandler<EodCommandsHandler>()
                .PublishingEvents(
                    typeof(SnapshotCreatedEvent),
                    typeof(SnapshotCreationFailedEvent)
                )
                .With(EventsRoute);
        }

        private IRegistration RegisterLiquidationSaga()
        {
            var sagaRegistration = RegisterSaga<LiquidationSaga>();

            sagaRegistration
                .PublishingCommands(
                    typeof(FailLiquidationInternalCommand),
                    typeof(FinishLiquidationInternalCommand),
                    typeof(LiquidatePositionsInternalCommand),
                    typeof(StartSpecialLiquidationInternalCommand)
                )
                .To(_settings.ContextNames.TradingEngine)
                .With(CommandsRoute)
                .ListeningEvents(
                    typeof(LiquidationFailedEvent),
                    typeof(LiquidationFinishedEvent),
                    typeof(LiquidationResumedEvent),
                    typeof(LiquidationStartedEvent),
                    typeof(NotEnoughLiquidityInternalEvent),
                    typeof(PositionsLiquidationFinishedInternalEvent),
                    typeof(MarketStateChangedEvent)
                )
                .From(_settings.ContextNames.TradingEngine)
                .On(EventsRoute);

            return sagaRegistration;
        }

        private void RegisterLiquidationCommandsHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningCommands(
                    typeof(StartLiquidationInternalCommand),
                    typeof(FailLiquidationInternalCommand),
                    typeof(FinishLiquidationInternalCommand),
                    typeof(LiquidatePositionsInternalCommand),
                    typeof(ResumeLiquidationInternalCommand)
                )
                .On(CommandsRoute)
                .WithCommandsHandler<LiquidationCommandsHandler>()
                .PublishingEvents(
                    typeof(LiquidationFailedEvent),
                    typeof(LiquidationFinishedEvent),
                    typeof(LiquidationResumedEvent),
                    typeof(LiquidationStartedEvent),
                    typeof(NotEnoughLiquidityInternalEvent),
                    typeof(PositionsLiquidationFinishedInternalEvent)
                )
                .With(EventsRoute);
        }

        private ISagaRegistration RegisterSaga<TSaga>()
        {
            return Register.Saga<TSaga>($"{_settings.ContextNames.TradingEngine}.{typeof(TSaga).Name}");
        }
    }
}