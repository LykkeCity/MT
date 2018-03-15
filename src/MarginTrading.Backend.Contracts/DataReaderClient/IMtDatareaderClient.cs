using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    [PublicAPI]
    public interface IMtDataReaderClient
    {
        IAssetPairSettingsReadingApi AssetPairSettingsRead { get; }
        IAccountAssetPairsReadingApi AccountAssetPairsRead { get; }
        IAccountHistoryApi AccountHistory { get; }
        IAccountsApi AccountsApi { get; }
        ITradingConditionsReadingApi TradingConditionsRead { get; }
    }
}