using System;
using System.Net.Http;
using MarginTrading.Backend.Contracts.Infrastructure;
using Refit;

namespace MarginTrading.Backend.Contracts.Client
{
    internal class MtBackendClient : IMtBackendClient
    {
        public IScheduleSettingsApi ScheduleSettings { get; }
        
        public IAccountsBalanceApi AccountsBalance { get; }
        
        public IAssetPairsEditingApi AssetPairsEdit { get; }

        public ITradingConditionsEditingApi TradingConditionsEdit { get; }

        public MtBackendClient(string url, string apiKey, string userAgent)
        {
            var httpMessageHandler = new ApiKeyHeaderHttpClientHandler(
                new UserAgentHeaderHttpClientHandler(
                    new RetryingHttpClientHandler(new HttpClientHandler(), 6, TimeSpan.FromSeconds(5)),
                    userAgent),
                apiKey);
            var settings = new RefitSettings {HttpMessageHandlerFactory = () => httpMessageHandler};
            
            ScheduleSettings = RestService.For<IScheduleSettingsApi>(url, settings);
            AccountsBalance = RestService.For<IAccountsBalanceApi>(url, settings);
            AssetPairsEdit = RestService.For<IAssetPairsEditingApi>(url, settings);
            TradingConditionsEdit = RestService.For<ITradingConditionsEditingApi>(url, settings);
        }
    }
}