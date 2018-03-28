using Autofac;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.MigrateApp
{
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        protected override string ApplicationName => "MarginTradingMigrateApp";

        public Startup(IHostingEnvironment env) : base(env)
        {
        }

        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder,
            IReloadingManager<Settings> settings, ILog log, bool isLive)
        {
            builder.RegisterType<Application>().As<IBrokerApplication>().SingleInstance();
        }
    }
}