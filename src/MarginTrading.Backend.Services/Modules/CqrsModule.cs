using System.Collections.Generic;
using System.Linq;
using Autofac;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Cqrs.Configuration.Routing;
using Lykke.Cqrs.Configuration.Saga;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Workflow;
using MarginTrading.SettingsService.Contracts.AssetPair;

namespace MarginTrading.Backend.Services.Modules
{
    public class CqrsModule : Module
    {
        private const string EventsRoute = "events";
        private const string CommandsRoute = "commands";
        private readonly CqrsSettings _settings;
        private readonly ILog _log;
        private readonly long _defaultRetryDelayMs;

        public CqrsModule(CqrsSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
            _defaultRetryDelayMs = (long) _settings.RetryDelay.TotalMilliseconds;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.ContextNames).AsSelf().SingleInstance();
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>()
                .SingleInstance();
            builder.RegisterType<CqrsSender>().As<ICqrsSender>().SingleInstance();

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
            return new CqrsEngine(_log, ctx.Resolve<IDependencyResolver>(), messagingEngine,
                new DefaultEndpointProvider(), true,
                Register.DefaultEndpointResolver(rabbitMqConventionEndpointResolver),
                RegisterDefaultRouting(),
                RegisterContext());
        }
        
        private IRegistration RegisterContext()
        {
            var contextRegistration = Register.BoundedContext(_settings.ContextNames.TradingEngine)
                .FailedCommandRetryDelay(_defaultRetryDelayMs).ProcessingOptions(CommandsRoute).MultiThreaded(8)
                .QueueCapacity(1024);

            contextRegistration.ListeningCommands(
                    typeof(FreezeAmountForWithdrawalCommand), 
                    typeof(UnfreezeMarginOnFailWithdrawalCommand))
                .On(CommandsRoute)
                .WithCommandsHandler<WithdrawalCommandsHandler>()
                .PublishingEvents(
                    typeof(AmountForWithdrawalFrozenEvent), 
                    typeof(AmountForWithdrawalFreezeFailedEvent))
                .With(EventsRoute);

            contextRegistration.ListeningEvents(
                    typeof(AccountChangedEvent))
                .From(_settings.ContextNames.AccountsManagement).On(EventsRoute)
                .WithProjection(
                    typeof(AccountsProjection), _settings.ContextNames.AccountsManagement);

            contextRegistration.ListeningEvents(
                    typeof(AssetPairChangedEvent))
                .From(_settings.ContextNames.SettingsService).On(EventsRoute)
                .WithProjection(
                    typeof(AssetPairProjection), _settings.ContextNames.SettingsService);
            
            contextRegistration.PublishingEvents(typeof(PositionClosedEvent)).With(EventsRoute);

            return contextRegistration;
        }

        private PublishingCommandsDescriptor<IDefaultRoutingRegistration> RegisterDefaultRouting()
        {
            return Register.DefaultRouting
                .PublishingCommands(
                    typeof(SuspendAssetPairCommand),
                    typeof(UnsuspendAssetPairCommand)
                )
                .To(_settings.ContextNames.SettingsService)
                .With(CommandsRoute);
        }
    }
}