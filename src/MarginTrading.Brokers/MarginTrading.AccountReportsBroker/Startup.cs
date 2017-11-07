using Autofac;
using Common.Log;
using MarginTrading.AccountReportsBroker.Repositories.AzureRepositories;
using MarginTrading.AccountReportsBroker.Repositories;
using MarginTrading.AccountReportsBroker.Repositories.SqlRepositories;
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
                        
            builder.RegisterInstance(new AccountsStatsReportsRepositoryAggregator(new IAccountsStatsReportsRepository[]
            {
                new AccountsStatsReportsRepository(settings, log),
                new AccountsStatsReportsSqlRepository(settings, log)
            }))
            .As<IAccountsStatsReportsRepository>();

            builder.RegisterInstance(new AccountsReportsRepositoryAggregator(new IAccountsReportsRepository[]
            {
                new AccountsReportsRepository(settings, log),
                new AccountsReportsSqlRepository(settings, log)
            }))
            .As<IAccountsReportsRepository>();

            builder.Register<IMarginTradingAccountStatsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountStatsRepository(settings.Db.HistoryConnString, log)
            ).SingleInstance();
        }
    }
}