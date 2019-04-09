using Autofac;
using Common.Log;
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
        private readonly ILog _log;

        public BaseServicesModule(MtBackendSettings mtSettings, ILog log)
        {
            _mtSettings = mtSettings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ThreadSwitcherToNewTask>()
                .As<IThreadSwitcher>()
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
