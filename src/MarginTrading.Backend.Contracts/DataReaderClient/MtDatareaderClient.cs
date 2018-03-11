using System;
using System.Net;
using System.Net.Http;
using MarginTrading.Backend.Contracts.Client;
using Refit;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    internal class MtDataReaderClient : IMtDataReaderClient
    {
        public IAssetPairSettingsReadingApi AssetPairSettingsRead { get; }
        public IAccountHistoryApi AccountHistory { get; }

        public MtDataReaderClient(string url, string apiKey, string userAgent)
        {
            var httpMessageHandler = new MtHeadersHttpClientHandler(
                new RetryingHttpClientHandler(new HttpClientHandler(), 6, TimeSpan.FromSeconds(5)), 
                userAgent, apiKey);
            var settings = new RefitSettings {HttpMessageHandlerFactory = () => httpMessageHandler};
            AssetPairSettingsRead = RestService.For<IAssetPairSettingsReadingApi>(url, settings);
            AccountHistory = RestService.For<IAccountHistoryApi>(url, settings);
        }
    }
}