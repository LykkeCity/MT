using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    [PublicAPI]
    public interface IMtDataReaderClientsPair
    {
        IMtDataReaderClient Demo { get; }
        IMtDataReaderClient Live { get; }
        IMtDataReaderClient Get(bool isLive);
    }
}