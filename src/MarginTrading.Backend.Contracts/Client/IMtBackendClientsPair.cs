using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Client
{
    [PublicAPI]
    internal interface IMtBackendClientsPair
    {
        IMtBackendClient Demo { get; }
        IMtBackendClient Live { get; }
    }
}