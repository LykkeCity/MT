using Autofac;
using Common.Log;
using MarginTrading.AccountHistoryBroker.AzureRepositories;
using MarginTrading.AzureRepositories;
using MarginTrading.BrokerBase;
using MarginTrading.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.AccountHistoryBroker
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

            builder.Register<IMarginTradingAccountHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountHistoryRepository(settings.Db.HistoryConnString, log)
            ).SingleInstance();

            builder.RegisterType<AccountTransactionsReportsRepository>().As<IAccountTransactionsReportsRepository>()
                .SingleInstance();
        }
    }
}