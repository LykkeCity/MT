using Autofac;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services.Modules
{
    public class SettingsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var settings = new MarginSettings
            {
                RabbitMqQueues = new RabbitMqQueues()
            };

            builder.RegisterInstance(settings).SingleInstance();
        }
    }
}
