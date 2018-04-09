using JetBrains.Annotations;
using Lykke.ClientGenerator;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.Backend.Contracts.Client
{
    public static class ServiceCollectionExtensions
    {
        [PublicAPI]
        public static void RegisterMtBackendClientsPair(this IServiceCollection services, ClientProxyGenerator demo,
            ClientProxyGenerator live)
        {
            services.AddSingleton<IMtBackendClientsPair>(p => new MtBackendClientsPair(
                new MtBackendClient(demo),
                new MtBackendClient(live)));
        }

        [PublicAPI]
        public static void RegisterMtBackendClient(this IServiceCollection services, ClientProxyGenerator clientProxyGenerator)
        {
            services.AddSingleton<IMtBackendClient>(p => new MtBackendClient(clientProxyGenerator));
        }
    }
}