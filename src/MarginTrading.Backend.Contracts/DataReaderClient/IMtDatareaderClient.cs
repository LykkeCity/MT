using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    [PublicAPI]
    public interface IMtDataReaderClient
    {
        IAssetPairSettingsReadingApi AssetPairSettingsRead { get; }
        IAccountHistoryApi AccountHistory { get; }
        IAccountsApi AccountsApi { get; }
        IAccountAssetPairsReadingApi AccountAssetPairsRead { get; }
        ITradeMonitoringReadingApi TradeMonitoringRead { get; }
        ITradingConditionsReadingApi TradingConditionsRead { get; }
    }
}