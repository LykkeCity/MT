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
using Lykke.Snow.Common.Correlation.Http;
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
                builder
                    .Register(ctx =>
                    {
                        var gavelClientGenerator = HttpClientGenerator
                            .BuildForUrl(_settings.CurrentValue.MtStpExchangeConnectorClient.ServiceUrl)
                            .WithAdditionalDelegatingHandler(ctx.Resolve<HttpCorrelationHandler>())
                            .WithServiceName<LykkeErrorResponse>(
                                $"Gavel [{_settings.CurrentValue.MtStpExchangeConnectorClient.ServiceUrl}]")
                            .WithApiKey(_settings.CurrentValue.MtStpExchangeConnectorClient.ApiKey)
                            .WithoutRetries()
                            .Create();
                        return gavelClientGenerator.Generate<IExchangeConnectorClient>();
                    })
                    .As<IExchangeConnectorClient>().SingleInstance();
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
                builder.RegisterClientAccountClient(_settings.CurrentValue.ClientAccountServiceClient.ServiceUrl);

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
            
            #region Asset Service

            builder
                .Register(ctx => BuildSettingsClientGenerator(ctx).Generate<IAssetsApi>())
                .As<IAssetsApi>().SingleInstance();
            
            builder
                .Register(ctx => BuildSettingsClientGenerator(ctx).Generate<IAssetPairsApi>())
                .As<IAssetPairsApi>().SingleInstance();
            
            builder
                .Register(ctx => BuildSettingsClientGenerator(ctx).Generate<ITradingConditionsApi>())
                .As<ITradingConditionsApi>().SingleInstance();
            
            builder
                .Register(ctx => BuildSettingsClientGenerator(ctx).Generate<ITradingInstrumentsApi>())
                .As<ITradingInstrumentsApi>().SingleInstance();
            
            builder
                .Register(ctx => BuildSettingsClientGenerator(ctx).Generate<IScheduleSettingsApi>())
                .As<IScheduleSettingsApi>().SingleInstance();
            
            builder
                .Register(ctx => BuildSettingsClientGenerator(ctx).Generate<ITradingRoutesApi>())
                .As<ITradingRoutesApi>().SingleInstance();
            
            builder
                .Register(ctx => BuildSettingsClientGenerator(ctx).Generate<IServiceMaintenanceApi>())
                .As<IServiceMaintenanceApi>().SingleInstance();

            builder
                .Register(ctx => BuildSettingsClientGenerator(ctx).Generate<IClientProfileSettingsApi>())
                .As<IClientProfileSettingsApi>().SingleInstance();

            #endregion

            #region MT Accounts Management

            builder
                .Register(ctx => BuildAccountManagementClientGenerator(ctx).Generate<IAccountsApi>())
                .As<IAccountsApi>().SingleInstance();
            
            builder
                .Register(ctx => BuildAccountManagementClientGenerator(ctx).Generate<IAccountBalanceHistoryApi>())
                .As<IAccountBalanceHistoryApi>().SingleInstance();

            #endregion

            #region Mdm

            builder
                .Register(ctx =>
                {
                    //for feature management
                    var mdmGenerator = HttpClientGenerator
                        .BuildForUrl(_settings.CurrentValue.MdmServiceClient.ServiceUrl)
                        .WithAdditionalDelegatingHandler(ctx.Resolve<HttpCorrelationHandler>())
                        .WithServiceName<LykkeErrorResponse>(
                            $"Mdm [{_settings.CurrentValue.MdmServiceClient.ServiceUrl}]");

                    if (!string.IsNullOrWhiteSpace(_settings.CurrentValue.MdmServiceClient.ApiKey))
                    {
                        mdmGenerator = mdmGenerator
                            .WithApiKey(_settings.CurrentValue.MdmServiceClient.ApiKey);
                    }
                    return mdmGenerator.Create().Generate<IBrokerSettingsApi>();
                })
                .As<IBrokerSettingsApi>().SingleInstance();

            #endregion

            #region OrderBook Service

            builder
                .Register(ctx =>
                {
                    var orderBookServiceClientGeneratorBuilder = HttpClientGenerator
                        .BuildForUrl(_settings.CurrentValue.OrderBookServiceClient.ServiceUrl)
                        .WithAdditionalDelegatingHandler(ctx.Resolve<HttpCorrelationHandler>())
                        .WithServiceName<LykkeErrorResponse>(
                            $"MT OrderBook Service [{_settings.CurrentValue.OrderBookServiceClient.ServiceUrl}]");
            
                    if (!string.IsNullOrWhiteSpace(_settings.CurrentValue.OrderBookServiceClient.ApiKey))
                    {
                        orderBookServiceClientGeneratorBuilder = orderBookServiceClientGeneratorBuilder
                            .WithApiKey(_settings.CurrentValue.OrderBookServiceClient.ApiKey);
                    }
                    return orderBookServiceClientGeneratorBuilder.Create().Generate<IOrderBookProviderApi>();
                })
                .As<IOrderBookProviderApi>().SingleInstance();

            #endregion OrderBook Service
        }

        private HttpClientGenerator BuildAccountManagementClientGenerator(IComponentContext ctx)
        {
            var accountManagementClientGeneratorBuilder = HttpClientGenerator
                .BuildForUrl(_settings.CurrentValue.AccountsManagementServiceClient.ServiceUrl)
                .WithAdditionalDelegatingHandler(ctx.Resolve<HttpCorrelationHandler>())
                .WithServiceName<LykkeErrorResponse>(
                    $"MT Account Management [{_settings.CurrentValue.AccountsManagementServiceClient.ServiceUrl}]");
            
            if (!string.IsNullOrWhiteSpace(_settings.CurrentValue.AccountsManagementServiceClient.ApiKey))
            {
                accountManagementClientGeneratorBuilder = accountManagementClientGeneratorBuilder
                    .WithApiKey(_settings.CurrentValue.AccountsManagementServiceClient.ApiKey);
            }

            return accountManagementClientGeneratorBuilder.Create();
        }

        private HttpClientGenerator BuildSettingsClientGenerator(IComponentContext ctx)
        {
            var settingsClientGeneratorBuilder = HttpClientGenerator
                .BuildForUrl(_settings.CurrentValue.SettingsServiceClient.ServiceUrl)
                .WithAdditionalDelegatingHandler(ctx.Resolve<HttpCorrelationHandler>())
                .WithServiceName<LykkeErrorResponse>(
                    $"MT Settings [{_settings.CurrentValue.SettingsServiceClient.ServiceUrl}]")
                .WithRetriesStrategy(new LinearRetryStrategy(TimeSpan.FromMilliseconds(300), 3));
            
            if (!string.IsNullOrWhiteSpace(_settings.CurrentValue.SettingsServiceClient.ApiKey))
            {
                settingsClientGeneratorBuilder = settingsClientGeneratorBuilder
                    .WithApiKey(_settings.CurrentValue.SettingsServiceClient.ApiKey);
            }

            return settingsClientGeneratorBuilder.Create();
        }
    }
}