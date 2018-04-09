using System;
using System.Net;
using System.Net.Http;
using MarginTrading.Backend.Contracts.Client;
using MarginTrading.Backend.Contracts.Infrastructure;
using Refit;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    internal class MtDataReaderClient : IMtDataReaderClient
    {
        public IAssetPairsReadingApi AssetPairsRead { get; }
        public IAccountHistoryApi AccountHistory { get; }
        public IAccountsApi AccountsApi { get; }
        public IAccountAssetPairsReadingApi AccountAssetPairsRead { get; }
        public ITradeMonitoringReadingApi TradeMonitoringRead { get; }
        public ITradingConditionsReadingApi TradingConditionsRead { get; }
        public IAccountGroupsReadingApi AccountGroups { get; }
        public IDictionariesReadingApi Dictionaries { get; }
        public IRoutesReadingApi Routes { get; }
        public ISettingsReadingApi Settings { get; }

        public MtDataReaderClient(string url, string apiKey, string userAgent)
        {
            var httpMessageHandler = new ApiKeyHeaderHttpClientHandler(
                new UserAgentHeaderHttpClientHandler(
                    new RetryingHttpClientHandler(new HttpClientHandler(), 6, TimeSpan.FromSeconds(5)),
                    userAgent), 
                apiKey);
            var settings = new RefitSettings {HttpMessageHandlerFactory = () => httpMessageHandler};
            AssetPairsRead = AddCaching(RestService.For<IAssetPairsReadingApi>(url, settings));
            AccountHistory = RestService.For<IAccountHistoryApi>(url, settings);
            AccountsApi = RestService.For<IAccountsApi>(url, settings);
            AccountAssetPairsRead = RestService.For<IAccountAssetPairsReadingApi>(url, settings);
            TradeMonitoringRead = RestService.For<ITradeMonitoringReadingApi>(url, settings);
            TradingConditionsRead = RestService.For<ITradingConditionsReadingApi>(url, settings);
            AccountGroups = RestService.For<IAccountGroupsReadingApi>(url, settings);
            Dictionaries = RestService.For<IDictionariesReadingApi>(url, settings);
            Routes = RestService.For<IRoutesReadingApi>(url, settings);
            Settings = RestService.For<ISettingsReadingApi>(url, settings);
        }

        private T AddCaching<T>(T obj)
        {
            var cachingHelper = new CachingHelper();
            return AopProxy.Create(obj, cachingHelper.HandleMethodCall);
        }
    }
}