using JetBrains.Annotations;
using Lykke.HttpClientGenerator;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.Backend.Contracts.Client
{
    public static class ServiceCollectionExtensions
    {
        [PublicAPI]
        public static void RegisterMtBackendClientsPair(this IServiceCollection services, IHttpClientGenerator demo,
            IHttpClientGenerator live)
        {
            services.AddSingleton<IMtBackendClientsPair>(p => new MtBackendClientsPair(
                new MtBackendClient(demo),
                new MtBackendClient(live)));
        }

        [PublicAPI]
        public static void RegisterMtBackendClient(this IServiceCollection services, IHttpClientGenerator clientProxyGenerator)
        {
            services.AddSingleton<IMtBackendClient>(p => new MtBackendClient(clientProxyGenerator));
        }
    }
}