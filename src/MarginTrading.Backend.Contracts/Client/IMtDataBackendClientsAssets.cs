using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Client
{
    [PublicAPI]
    public interface IMtBackendClientsAssets
    {
        IMtBackendClient Demo { get; }
        IMtBackendClient Live { get; }

        IMtBackendClient Get(bool isLive);
    }
}