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
                new SrvAppNotifications(_settings.Notifications.ConnString, _settings.Notifications.HubName)
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
