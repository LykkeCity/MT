using Lykke.HttpClientGenerator;

namespace MarginTrading.Backend.Contracts.Client
{
    internal class MtBackendClient : IMtBackendClient
    {
        public IOrdersApi Orders { get; }
        
        public IPositionsApi Positions { get; }

        public MtBackendClient(IHttpClientGenerator clientProxyGenerator)
        {
            Orders = clientProxyGenerator.Generate<IOrdersApi>();
            Positions = clientProxyGenerator.Generate<IPositionsApi>();
        }

        
    }
}