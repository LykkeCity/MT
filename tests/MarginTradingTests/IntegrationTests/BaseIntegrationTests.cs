using System;
using Autofac;
using Lykke.Service.Session.AutorestClient;
using MarginTrading.AzureRepositories;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Services;
using MarginTradingTests.IntegrationTests.Client;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace MarginTradingTests.IntegrationTests
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
               AzureRepoFactories.Clients.CreateClientsRepository(configuration["ClientInfoConnString"], null)
            ).SingleInstance();

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
