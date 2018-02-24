using Autofac;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.ExternalOrderBroker.Repositories;
using MarginTrading.ExternalOrderBroker.Repositories.Azure;
using MarginTrading.ExternalOrderBroker.Repositories.Sql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.ExternalOrderBroker
{
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        public Startup(IHostingEnvironment env) : base(env)
        {
        }

        protected override string ApplicationName => "ExternalOrderBroker";

        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder,
            IReloadingManager<Settings> settings, ILog log, bool isLive)
        {
            builder.RegisterType<Application>().As<IBrokerApplication>().SingleInstance();

            builder.RegisterInstance(new ReportRepositoryAggregator(new IExternalOrderReportRepository[]
                {
                    new ExternalOrderReportAzureRepository(settings, log),
                    new ExternalOrderReportSqlRepository(settings.CurrentValue, log),
                }))
                .As<IExternalOrderReportRepository>();
        }
    }
}