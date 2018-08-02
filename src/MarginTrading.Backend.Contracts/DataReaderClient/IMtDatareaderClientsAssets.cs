using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    [PublicAPI]
    public interface IMtDataReaderClientsAssets
    {
        IMtDataReaderClient Demo { get; }
        IMtDataReaderClient Live { get; }
        IMtDataReaderClient Get(bool isLive);
    }
}