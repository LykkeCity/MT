using Autofac;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.OrderHistoryBroker.Repositories.SqlRepositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.OrderHistoryBroker
{
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        public Startup(IHostingEnvironment env) : base(env)
        {
        }

        protected override string ApplicationName => "MarginTradingOrderHistoryBroker";

        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder, IReloadingManager<Settings> settings, ILog log, bool isLive)
        {
            builder.RegisterType<OrderHistoryApplication>().As<IBrokerApplication>().SingleInstance();
            builder.RegisterType<TradesApplication>().As<IBrokerApplication>().SingleInstance();

            builder.Register<IMarginTradingOrdersHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateOrdersHistoryRepository(settings.Nested(s => s.Db.HistoryConnString), log)
            ).SingleInstance();
            builder.Register<IMarginTradingOrdersHistoryRepository>(ctx =>
                new MarginTradingOrdersHistorySqlRepository(settings.CurrentValue, log)
            ).SingleInstance();
        }
    }
}