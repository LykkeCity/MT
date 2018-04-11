using Lykke.HttpClientGenerator;

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

        public MtDataReaderClient(IHttpClientGenerator clientGenerator)
        {
            AssetPairsRead = clientGenerator.Generate<IAssetPairsReadingApi>();
            AccountHistory = clientGenerator.Generate<IAccountHistoryApi>();
            AccountsApi = clientGenerator.Generate<IAccountsApi>();
            AccountAssetPairsRead = clientGenerator.Generate<IAccountAssetPairsReadingApi>();
            TradeMonitoringRead = clientGenerator.Generate<ITradeMonitoringReadingApi>();
            TradingConditionsRead = clientGenerator.Generate<ITradingConditionsReadingApi>();
            AccountGroups = clientGenerator.Generate<IAccountGroupsReadingApi>();
            Dictionaries = clientGenerator.Generate<IDictionariesReadingApi>();
            Routes = clientGenerator.Generate<IRoutesReadingApi>();
            Settings = clientGenerator.Generate<ISettingsReadingApi>();
        }
    }
}