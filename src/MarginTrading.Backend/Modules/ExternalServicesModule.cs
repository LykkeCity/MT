// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Autofac;
using Common.Log;
using Lykke.HttpClientGenerator;
using Lykke.HttpClientGenerator.Retries;
using Lykke.MarginTrading.OrderBookService.Contracts;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.EmailSender;
using Lykke.SettingsReader;
using Lykke.Snow.Common.Startup;
using Lykke.Snow.Mdm.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.Backend.Contracts.ExchangeConnector;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Infrastructure;
using MarginTrading.Backend.Services.FakeExchangeConnector;
using MarginTrading.Backend.Services.Settings;
using MarginTrading.Backend.Services.Stubs;
using MarginTrading.Common.Services.Client;
using MarginTrading.AssetService.Contracts;

namespace MarginTrading.Backend.Modules
{
    public class ExternalServicesModule : Module
    {
        private readonly IReloadingManager<MtBackendSettings> _settings;

        public ExternalServicesModule(IReloadingManager<MtBackendSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (_settings.CurrentValue.MtBackend.ExchangeConnector == ExchangeConnectorType.RealExchangeConnector)
            {
                var gavelClientGenerator = HttpClientGenerator
                    .BuildForUrl(_settings.CurrentValue.MtStpExchangeConnectorClient.ServiceUrl)
                    .WithServiceName<LykkeErrorResponse>(
                        $"Gavel [{_settings.CurrentValue.MtStpExchangeConnectorClient.ServiceUrl}]")
                    .WithApiKey(_settings.CurrentValue.MtStpExchangeConnectorClient.ApiKey)
                    .WithoutRetries()
                    .Create();
                    
                builder.RegisterInstance(gavelClientGenerator.Generate<IExchangeConnectorClient>())
                .As<IExchangeConnectorClient>()
                .SingleInstance();
            }
            if (_settings.CurrentValue.MtBackend.ExchangeConnector == ExchangeConnectorType.FakeExchangeConnector)
            {
                builder.RegisterType<FakeExchangeConnectorClient>()
                    .As<IExchangeConnectorClient>()
                    .SingleInstance();
            }
            
            #region Client Account Service
            
            if (_settings.CurrentValue.ClientAccountServiceClient != null)
            {
                builder.RegisterLykkeServiceClient(_settings.CurrentValue.ClientAccountServiceClient.ServiceUrl);
                
                builder.RegisterType<ClientAccountService>()
                    .As<IClientAccountService>()
                    .SingleInstance();
            }
            else
            {
                builder.RegisterType<ClientAccountServiceEmptyStub>()
                    .As<IClientAccountService>()
                    .SingleInstance();
            }
            
            #endregion
            
            #region Email Sender

            if (_settings.CurrentValue.EmailSender != null)
            {
                builder.Register<IEmailSender>(ctx =>
                    new EmailSenderClient(_settings.CurrentValue.EmailSender.ServiceUrl, ctx.Resolve<ILog>())
                ).SingleInstance();
            }
            else
            {
                builder.RegisterType<EmailSenderLogStub>().As<IEmailSender>().SingleInstance();
            }
            
            #endregion
            
            #region MT Settings

            var settingsClientGeneratorBuilder = HttpClientGenerator
                .BuildForUrl(_settings.CurrentValue.SettingsServiceClient.ServiceUrl)
                .WithServiceName<LykkeErrorResponse>(
                    $"MT Settings [{_settings.CurrentValue.SettingsServiceClient.ServiceUrl}]")
                .WithRetriesStrategy(new LinearRetryStrategy(TimeSpan.FromMilliseconds(300), 3));
            
            if (!string.IsNullOrWhiteSpace(_settings.CurrentValue.SettingsServiceClient.ApiKey))
            {
                settingsClientGeneratorBuilder = settingsClientGeneratorBuilder
                    .WithApiKey(_settings.CurrentValue.SettingsServiceClient.ApiKey);
            }

            var settingsClientGenerator = settingsClientGeneratorBuilder.Create();
                
            builder.RegisterInstance(settingsClientGenerator.Generate<IAssetsApi>())
                .As<IAssetsApi>().SingleInstance();
            
            builder.RegisterInstance(settingsClientGenerator.Generate<IAssetPairsApi>())
                .As<IAssetPairsApi>().SingleInstance();
            
            builder.RegisterInstance(settingsClientGenerator.Generate<ITradingConditionsApi>())
                .As<ITradingConditionsApi>().SingleInstance();
            
            builder.RegisterInstance(settingsClientGenerator.Generate<ITradingInstrumentsApi>())
                .As<ITradingInstrumentsApi>().SingleInstance();
            
            builder.RegisterInstance(settingsClientGenerator.Generate<IScheduleSettingsApi>())
                .As<IScheduleSettingsApi>().SingleInstance();
            
            builder.RegisterInstance(settingsClientGenerator.Generate<ITradingRoutesApi>())
                .As<ITradingRoutesApi>().SingleInstance();
            
            builder.RegisterInstance(settingsClientGenerator.Generate<IServiceMaintenanceApi>())
                .As<IServiceMaintenanceApi>().SingleInstance();

            #endregion

            #region MT Accounts Management

            var accountsClientGeneratorBuilder = HttpClientGenerator
                .BuildForUrl(_settings.CurrentValue.AccountsManagementServiceClient.ServiceUrl)
                .WithServiceName<LykkeErrorResponse>(
                    $"MT Account Management [{_settings.CurrentValue.AccountsManagementServiceClient.ServiceUrl}]");
            
            if (!string.IsNullOrWhiteSpace(_settings.CurrentValue.AccountsManagementServiceClient.ApiKey))
            {
                accountsClientGeneratorBuilder = accountsClientGeneratorBuilder
                    .WithApiKey(_settings.CurrentValue.AccountsManagementServiceClient.ApiKey);
            }

            builder.RegisterInstance(accountsClientGeneratorBuilder.Create().Generate<IAccountsApi>())
                .As<IAccountsApi>().SingleInstance();

            #endregion

            #region Mdm

            //for feature management
            var mdmGenerator = HttpClientGenerator
                .BuildForUrl(_settings.CurrentValue.MdmServiceClient.ServiceUrl)
                .WithServiceName<LykkeErrorResponse>(
                    $"Mdm [{_settings.CurrentValue.MdmServiceClient.ServiceUrl}]");

            if (!string.IsNullOrWhiteSpace(_settings.CurrentValue.MdmServiceClient.ApiKey))
            {
                mdmGenerator = mdmGenerator
                    .WithApiKey(_settings.CurrentValue.MdmServiceClient.ApiKey);
            }

            builder.RegisterInstance(mdmGenerator.Create().Generate<IBrokerSettingsApi>())
                .As<IBrokerSettingsApi>().SingleInstance();

            #endregion

            #region OrderBook Service

            var orderBookServiceClientGeneratorBuilder = HttpClientGenerator
                .BuildForUrl(_settings.CurrentValue.OrderBookServiceClient.ServiceUrl)
                .WithServiceName<LykkeErrorResponse>(
                    $"MT OrderBook Service [{_settings.CurrentValue.OrderBookServiceClient.ServiceUrl}]");
            
            if (!string.IsNullOrWhiteSpace(_settings.CurrentValue.OrderBookServiceClient.ApiKey))
            {
                orderBookServiceClientGeneratorBuilder = orderBookServiceClientGeneratorBuilder
                    .WithApiKey(_settings.CurrentValue.OrderBookServiceClient.ApiKey);
            }

            builder.RegisterInstance(orderBookServiceClientGeneratorBuilder.Create().Generate<IOrderBookProviderApi>())
                .As<IOrderBookProviderApi>()
                .SingleInstance();

            #endregion OrderBook Service
        }
    }
}