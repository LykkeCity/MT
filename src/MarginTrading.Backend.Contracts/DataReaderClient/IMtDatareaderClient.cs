using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    [PublicAPI]
    public interface IMtDataReaderClient
    {
        IAssetPairSettingsReadingApi AssetPairSettingsRead { get; }
        IAccountHistoryApi AccountHistory { get; }
    }
}