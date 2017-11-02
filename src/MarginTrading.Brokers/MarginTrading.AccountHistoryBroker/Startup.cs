using Autofac;
using Common.Log;
using MarginTrading.AccountHistoryBroker.AzureRepositories;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.AccountHistoryBroker
{
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        protected override string ApplicationName => "MarginTradingAccountHistoryBroker";

        public Startup(IHostingEnvironment env) : base(env)
        {
        }


        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder,
            Settings settings, ILog log, bool isLive)
        {
            builder.RegisterType<Application>().As<IBrokerApplication>().SingleInstance();

            builder.Register<IMarginTradingAccountHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountHistoryRepository(settings.Db.HistoryConnString, log)
            ).SingleInstance();

            builder.RegisterType<AccountTransactionsReportsRepository>().As<IAccountTransactionsReportsRepository>()
                .SingleInstance();
        }
    }
}