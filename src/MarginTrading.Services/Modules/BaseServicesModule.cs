using Autofac;
using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using MarginTrading.Services.Notifications;

namespace MarginTrading.Services.Modules
{
    public class BaseServicesModule : Module
    {
        private readonly MtBackendSettings _mtSettings;

        public BaseServicesModule(MtBackendSettings mtSettings)
        {
            _mtSettings = mtSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ThreadSwitcherToNewTask>()
                .As<IThreadSwitcher>()
                .SingleInstance();

            builder.Register<IAppNotifications>(ctx =>
                new SrvAppNotifications(_mtSettings.Jobs.NotificationsHubConnectionString, _mtSettings.Jobs.NotificationsHubName)
            ).SingleInstance();

            builder.RegisterType<ClientNotifyService>()
                .As<IClientNotifyService>()
                .SingleInstance();

            builder.RegisterType<RabbitMqNotifyService>()
                .As<IRabbitMqNotifyService>()
                .SingleInstance();

            builder.RegisterType<DateService>()
                .As<IDateService>()
                .SingleInstance();
        }
    }
}
