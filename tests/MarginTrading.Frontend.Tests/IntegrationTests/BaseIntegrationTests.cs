using System;
using Autofac;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Lykke.Service.Session.AutorestClient;
using MarginTrading.Common.Settings;
using MarginTrading.Common.Settings.Repositories;
using MarginTrading.Common.Settings.Repositories.Azure;
using MarginTrading.Common.Settings.Repositories.Azure.Entities;
using MarginTrading.Frontend.Tests.IntegrationTests.Client;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace MarginTrading.Frontend.Tests.IntegrationTests
{
    public class BaseIntegrationTests
    {
        protected IContainer Container { get; set; }

        protected void RegisterDependencies()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(TestContext.CurrentContext.TestDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("appsettings.dev.json", true, true)
                .Build();

            var builder = new ContainerBuilder();

            builder.Register<ISessionService>(ctx =>
                new SessionService(new Uri(configuration["SessionServiceApiUrl"]))
            ).SingleInstance();

            builder.Register<IClientAccountsRepository>(ctx =>
                new ClientsRepository(
                    AzureTableStorage<ClientAccountEntity>.Create(
                        configuration["ClientInfoConnString"].MakeSettings(), "Traders", null),
                    AzureTableStorage<AzureIndex>.Create(
                        configuration["ClientInfoConnString"].MakeSettings(), "Traders", null)));

            builder.RegisterType<ClientAccountService>()
                .As<IClientAccountService>()
                .SingleInstance();

            builder.RegisterType<MtClient>()
                .AsSelf()
                .SingleInstance();

            Container = builder.Build();
        }
    }
}
