using Autofac;
using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using MarginTrading.Services.Notifications;

namespace MarginTrading.Services.Modules
{
    public class BaseServicesModule : Module
    {
        private readonly MarginSettings _settings;

        public BaseServicesModule(MarginSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ThreadSwitcherToNewTask>()
                .As<IThreadSwitcher>()
                .SingleInstance();

            builder.Register<IAppNotifications>(ctx =>
                new SrvAppNotifications(_settings.SlackNotifications.AzureQueue.ConnectionString, _settings.SlackNotifications.AzureQueue.QueueName)
            ).SingleInstance();

            builder.RegisterType<ClientNotifyService>()
                .As<IClientNotifyService>()
                .SingleInstance();

            builder.RegisterType<RabbitMqNotifyService>()
                .As<IRabbitMqNotifyService>()
                .SingleInstance();
        }
    }
}
