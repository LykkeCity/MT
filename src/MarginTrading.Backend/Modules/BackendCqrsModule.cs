using System.Collections.Generic;
using Autofac;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using Lykke.Service.PushNotifications.Contract;
using Lykke.Service.PushNotifications.Contract.Commands;
using Lykke.SettingsReader;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Modules
{
    public class BackendCqrsModule : Module
    {
        private readonly IReloadingManager<MarginSettings> _settings;
        private readonly ILog _log;

        public BackendCqrsModule(IReloadingManager<MarginSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }
        
        protected override void Load(ContainerBuilder builder)
        {
            MessagePackSerializerFactory.Defaults.FormatterResolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory { Uri = _settings.CurrentValue.Cqrs.RabbitConnectionString };

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>();

            builder.Register(c => new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {"RabbitMq", new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName, rabbitMqSettings.Password, "None", "RabbitMq")}
                }),
                new RabbitMqTransportFactory()));

            builder.Register(ctx => new CqrsEngine(_log,
                    new AutofacDependencyResolver(ctx.Resolve<IComponentContext>()),
                    ctx.Resolve<MessagingEngine>(),
                    new DefaultEndpointProvider(),
                    true,
                    Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                        "RabbitMq",
                        SerializationFormat.MessagePack,
                        environment: "lykke",
                        exclusiveQueuePostfix: "k8s")),
                
                    Register.BoundedContext("mt-backend")
                        .PublishingCommands(
                            typeof(MtOrderChangedNotificationCommand)
                        )
                        .To(PushNotificationsBoundedContext.Name)
                        .With("commands")
                ))
                .As<ICqrsEngine>()
                .SingleInstance();
        }
    }
}
