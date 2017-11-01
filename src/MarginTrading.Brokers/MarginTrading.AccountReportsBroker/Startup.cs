using Autofac;
using Common.Log;
using MarginTrading.AccountReportsBroker.AzureRepositories;
using MarginTrading.AzureRepositories;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.AccountReportsBroker
{
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        protected override string ApplicationName => "MarginTradingAccountReportsBroker";

        public Startup(IHostingEnvironment env) : base(env)
        {
        }


        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder,
            Settings settings, ILog log, bool isLive)
        {
            builder.RegisterType<AccountStatReportsApplication>().As<IBrokerApplication>().SingleInstance();
            builder.RegisterType<AccountReportsApplication>().As<IBrokerApplication>().SingleInstance();

            builder.RegisterType<AccountsStatsReportsRepository>().As<IAccountsStatsReportsRepository>()
                .SingleInstance();
            
            builder.RegisterType<AccountsReportsRepository>().As<IAccountsReportsRepository>()
                .SingleInstance();

            builder.Register<IMarginTradingAccountStatsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountStatsRepository(settings.Db.HistoryConnString, log)
            ).SingleInstance();
        }
    }
}