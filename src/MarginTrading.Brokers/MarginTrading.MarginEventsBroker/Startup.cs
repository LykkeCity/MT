using Autofac;
using Common.Log;
using MarginTrading.BrokerBase;
using MarginTrading.MarginEventsBroker.AzureRepositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.MarginEventsBroker
{
    public class Startup : BrokerStartupBase<Settings>
    {
        protected override string ApplicationName => "MarginTradingAccountHistoryBroker";

        public Startup(IHostingEnvironment env) : base(env)
        {
        }


        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder,
            Settings settingsRoot, ILog log, bool isLive)
        {
            var settings = isLive ? settingsRoot.MtBackend.MarginTradingLive : settingsRoot.MtBackend.MarginTradingDemo;
            settings.IsLive = isLive;
            builder.RegisterInstance(settings).SingleInstance();
            builder.RegisterType<Application>().As<IBrokerApplication>().SingleInstance();

            builder.RegisterType<MarginEventsReportsRepository>().As<IMarginEventsReportsRepository>()
                .SingleInstance();
        }
    }
}