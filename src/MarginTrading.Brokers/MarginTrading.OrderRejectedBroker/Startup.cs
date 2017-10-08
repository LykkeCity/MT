using Autofac;
using Common.Log;
using MarginTrading.AzureRepositories;
using MarginTrading.BrokerBase;
using MarginTrading.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.OrderRejectedBroker
{
    public class Startup : BrokerStartupBase<Settings>
    {
        public Startup(IHostingEnvironment env) : base(env)
        {
        }

        protected override string ApplicationName => "MarginTradingOrderRejectedBroker";

        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder,
            Settings settingsRoot, ILog log,
            bool isLive)
        {
            var settings = isLive ? settingsRoot.MtBackend.MarginTradingLive : settingsRoot.MtBackend.MarginTradingDemo;
            settings.IsLive = isLive;
            builder.RegisterInstance(settings).SingleInstance();
            builder.RegisterType<Application>().As<IBrokerApplication>().SingleInstance();

            builder.Register<IMarginTradingOrdersRejectedRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateOrdersRejectedRepository(settings.Db.HistoryConnString, log)
            ).SingleInstance();
        }
    }
}