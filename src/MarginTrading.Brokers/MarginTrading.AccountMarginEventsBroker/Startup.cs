using Autofac;
using Common.Log;
using MarginTrading.AccountMarginEventsBroker.AzureRepositories;
using MarginTrading.BrokerBase;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.AccountMarginEventsBroker
{
    public class Startup : BrokerStartupBase<Settings>
    {
        protected override string ApplicationName => "MarginTradingAccountMarginEventsBroker";

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

            builder.RegisterType<AccountMarginEventsReportsRepository>().As<IAccountMarginEventsReportsRepository>()
                .SingleInstance();
        }
    }
}