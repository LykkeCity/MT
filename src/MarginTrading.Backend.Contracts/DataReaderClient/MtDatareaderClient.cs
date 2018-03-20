using MarginTrading.Backend.Contracts.Client;
using Refit;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    internal class MtDataReaderClient : IMtDataReaderClient
    {
        public IAssetPairSettingsReadingApi AssetPairSettingsRead { get; }
        public IAccountAssetPairsReadingApi AccountAssetPairsRead { get; }
        public IAccountHistoryApi AccountHistory { get; }
        public IAccountsApi AccountsApi { get; }
        public ITradingConditionsReadingApi TradingConditionsRead { get; }

        public MtDataReaderClient(string url, string apiKey, string userAgent)
        {
            var httpMessageHandler = new MtBackendHttpClientHandler(userAgent, apiKey);
            var settings = new RefitSettings {HttpMessageHandlerFactory = () => httpMessageHandler};
            AssetPairSettingsRead = RestService.For<IAssetPairSettingsReadingApi>(url, settings);
            AccountAssetPairsRead = RestService.For<IAccountAssetPairsReadingApi>(url, settings);
            AccountHistory = RestService.For<IAccountHistoryApi>(url, settings);
            AccountsApi = RestService.For<IAccountsApi>(url, settings);
            TradingConditionsRead = RestService.For<ITradingConditionsReadingApi>(url, settings);
        }
    }
}