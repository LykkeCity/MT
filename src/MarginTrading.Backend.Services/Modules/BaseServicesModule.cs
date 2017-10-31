using Autofac;
using Lykke.Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.Settings;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Modules
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
