using System;
using Autofac;
using Autofac.Core;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AccountReportsBroker.Repositories.AzureRepositories;
using MarginTrading.AccountReportsBroker.Repositories;
using MarginTrading.AccountReportsBroker.Repositories.SqlRepositories;
using MarginTrading.AzureRepositories;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.AccountReportsBroker
{
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        protected override string ApplicationName => "MarginTradingAccountReportsBroker";

        public Startup(IHostingEnvironment env) : base(env)
        {
        }

        protected override void SetSettingValues(Settings source, IConfigurationRoot configuration)
        {
            base.SetSettingValues(source, configuration);

            source.ReportTarget = GetReportTarget(configuration);
        }

        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder, IReloadingManager<Settings> settings, ILog log, bool isLive)
        {
            var settingsValue = settings.CurrentValue;

            if (settingsValue.ReportTarget == ReportTarget.All || settingsValue.ReportTarget == ReportTarget.Azure)
            {
                builder.RegisterType<AccountStatAzureReportsApplication>()
                    .As<IBrokerApplication>()
                    .WithParameter(
                        new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(IAccountsStatsReportsRepository),
                            (pi, ctx) => new AccountsStatsReportsRepository(settings, log)))
                    .SingleInstance();
            }
            
            if (settingsValue.ReportTarget == ReportTarget.All || settingsValue.ReportTarget == ReportTarget.Sql)
            {
                builder.RegisterType<AccountStatSqlReportsApplication>()
                    .As<IBrokerApplication>()
                    .WithParameter(
                        new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(IAccountsStatsReportsRepository),
                            (pi, ctx) => new AccountsStatsReportsSqlRepository(settings.CurrentValue, log)))
                    .SingleInstance();
            }
            
            builder.RegisterType<AccountReportsApplication>().As<IBrokerApplication>().SingleInstance();

            builder.RegisterInstance(new AccountsReportsRepositoryAggregator(new IAccountsReportsRepository[]
            {
                new AccountsReportsSqlRepository(settings.CurrentValue, log),
                new AccountsReportsRepository(settings, log)
            }))
            .As<IAccountsReportsRepository>();

            builder.Register<IMarginTradingAccountStatsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountStatsRepository(settings.Nested(s => s.Db.HistoryConnString), log)
            ).SingleInstance();
        }

        private static ReportTarget GetReportTarget(IConfigurationRoot configuration)
        {
            return Enum.TryParse(configuration["ReportTarget"], out ReportTarget result)
                ? result
                : ReportTarget.All;
        }
    }
}