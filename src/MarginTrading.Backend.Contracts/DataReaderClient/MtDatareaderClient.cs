using System;
using System.Net;
using System.Net.Http;
using MarginTrading.Backend.Contracts.Client;
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

        public MtDataReaderClient(string url, string apiKey, string userAgent)
        {
            var httpMessageHandler = new MtHeadersHttpClientHandler(
                new RetryingHttpClientHandler(new HttpClientHandler(), 6, TimeSpan.FromSeconds(5)), 
                userAgent, apiKey);
            var settings = new RefitSettings {HttpMessageHandlerFactory = () => httpMessageHandler};
            AssetPairsRead = RestService.For<IAssetPairsReadingApi>(url, settings);
            AccountHistory = RestService.For<IAccountHistoryApi>(url, settings);
            AccountsApi = RestService.For<IAccountsApi>(url, settings);
            AccountAssetPairsRead = RestService.For<IAccountAssetPairsReadingApi>(url, settings);
            TradeMonitoringRead = RestService.For<ITradeMonitoringReadingApi>(url, settings);
            TradingConditionsRead = RestService.For<ITradingConditionsReadingApi>(url, settings);
        }
    }
}