using Autofac;
using Common.Log;
using MarginTrading.AccountMarginEventsBroker.Repositories;
using MarginTrading.AccountMarginEventsBroker.Repositories.AzureRepositories;
using MarginTrading.AccountMarginEventsBroker.Repositories.SqlRepositories;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.AccountMarginEventsBroker
{
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        protected override string ApplicationName => "MarginTradingAccountMarginEventsBroker";

        public Startup(IHostingEnvironment env) : base(env)
        {
        }


        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder,
            Settings settings, ILog log, bool isLive)
        {
            builder.RegisterType<Application>().As<IBrokerApplication>().SingleInstance();
                     
            builder.RegisterInstance(new RepositoryAggregator(new IAccountMarginEventsReportsRepository[]
            {
                new AccountMarginEventsReportsSqlRepository(settings, log),
                new AccountMarginEventsReportsRepository(settings, log)
            }))
            .As<IAccountMarginEventsReportsRepository>();
        }
    }
}