using Autofac;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services.Modules
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
