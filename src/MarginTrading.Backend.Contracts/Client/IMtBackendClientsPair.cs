using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Client
{
    [PublicAPI]
    public interface IMtBackendClientsPair
    {
        IMtBackendClient Demo { get; }
        IMtBackendClient Live { get; }
    }
}