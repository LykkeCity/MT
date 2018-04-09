using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    [PublicAPI]
    public interface IMtDataReaderClient
    {
        IAssetPairsReadingApi AssetPairsRead { get; }
        IAccountHistoryApi AccountHistory { get; }
        IAccountsApi AccountsApi { get; }
        IAccountAssetPairsReadingApi AccountAssetPairsRead { get; }
        ITradeMonitoringReadingApi TradeMonitoringRead { get; }
        ITradingConditionsReadingApi TradingConditionsRead { get; }
        IAccountGroupsReadingApi AccountGroups { get; }
        IDictionariesReadingApi Dictionaries { get; }
        IRoutesReadingApi Routes { get; }
        ISettingsReadingApi Settings { get; }
    }
}