using Autofac;
using Common.Log;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.OrderRejectedBroker
{
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        public Startup(IHostingEnvironment env) : base(env)
        {
        }

        protected override string ApplicationName => "MarginTradingOrderRejectedBroker";

        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder,
            Settings settings, ILog log, bool isLive)
        {
            builder.RegisterType<Application>().As<IBrokerApplication>().SingleInstance();

            builder.Register<IMarginTradingOrdersRejectedRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateOrdersRejectedRepository(settings.Db.HistoryConnString, log)
            ).SingleInstance();
        }
    }
}