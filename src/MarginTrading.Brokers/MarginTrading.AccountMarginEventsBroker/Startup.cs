using Autofac;
using Common.Log;
using JetBrains.Annotations;
using Lykke.SettingsReader;
using MarginTrading.AccountMarginEventsBroker.Repositories;
using MarginTrading.AccountMarginEventsBroker.Repositories.AzureRepositories;
using MarginTrading.AccountMarginEventsBroker.Repositories.SqlRepositories;
using Lykke.MarginTrading.BrokerBase;
using Lykke.MarginTrading.BrokerBase.Models;
using Lykke.MarginTrading.BrokerBase.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.AccountMarginEventsBroker
{
    [UsedImplicitly]
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        protected override string ApplicationName => "AccountMarginEventsBroker";

        public Startup(IHostingEnvironment env) : base(env)
        {
        }
        
        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder, IReloadingManager<Settings> settings,
            ILog log)
        {
            builder.RegisterType<Application>().As<IBrokerApplication>().SingleInstance();

            if (settings.CurrentValue.Db.StorageMode == StorageMode.Azure)
            {
                builder.RegisterInstance(new AccountMarginEventsRepository(settings, log))
                    .As<IAccountMarginEventsRepository>();
            }
            else if (settings.CurrentValue.Db.StorageMode == StorageMode.SqlServer)
            {
                builder.RegisterInstance(new AccountMarginEventsSqlRepository(settings.CurrentValue, log))
                .As<IAccountMarginEventsRepository>();
            }
        }
    }
}