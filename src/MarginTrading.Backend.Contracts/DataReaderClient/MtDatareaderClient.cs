using MarginTrading.Backend.Contracts.Client;
using Refit;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    internal class MtDataReaderClient : IMtDataReaderClient
    {
        public IAssetPairSettingsReadingApi AssetPairSettingsRead { get; }
        public IAccountHistoryApi AccountHistory { get; }
        public IAccountsApi AccountsApi { get; }

        public MtDataReaderClient(string url, string apiKey, string userAgent)
        {
            var httpMessageHandler = new MtBackendHttpClientHandler(userAgent, apiKey);
            var settings = new RefitSettings {HttpMessageHandlerFactory = () => httpMessageHandler};
            AssetPairSettingsRead = RestService.For<IAssetPairSettingsReadingApi>(url, settings);
            AccountHistory = RestService.For<IAccountHistoryApi>(url, settings);
            AccountsApi = RestService.For<IAccountsApi>(url, settings);
        }
    }
}