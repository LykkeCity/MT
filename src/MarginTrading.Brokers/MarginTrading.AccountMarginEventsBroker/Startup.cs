// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Autofac;
using JetBrains.Annotations;
using Lykke.SettingsReader;
using MarginTrading.AccountMarginEventsBroker.Repositories;
using MarginTrading.AccountMarginEventsBroker.Repositories.SqlRepositories;
using Lykke.MarginTrading.BrokerBase;
using Lykke.MarginTrading.BrokerBase.Models;
using Lykke.MarginTrading.BrokerBase.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountMarginEventsBroker
{
    [UsedImplicitly]
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        protected override string ApplicationName => "AccountMarginEventsBroker";

        public Startup(IHostEnvironment env, IConfiguration configuration) : base(env, configuration)
        {
        }

        protected override void RegisterCustomServices(
            ContainerBuilder builder,
            IReloadingManager<Settings> settings)
        {
            builder
                .RegisterType<Application>()
                .As<IBrokerApplication>()
                .SingleInstance();

            switch (settings.CurrentValue.Db.StorageMode)
            {
                case StorageMode.Azure:
                    throw new NotImplementedException("Azure storage is not supported");
                case StorageMode.SqlServer:
                    builder.Register(c => new AccountMarginEventsSqlRepository(settings.CurrentValue,
                            c.Resolve<ILogger<AccountMarginEventsSqlRepository>>()))
                        .As<IAccountMarginEventsRepository>();
                    break;
            }
        }
    }
}