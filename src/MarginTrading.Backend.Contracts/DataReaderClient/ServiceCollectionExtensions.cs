using JetBrains.Annotations;
using Lykke.HttpClientGenerator;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    public static class ServiceCollectionExtensions
    {
        [PublicAPI]
        public static void RegisterMtDataReaderClientsPair(this IServiceCollection services, IHttpClientGenerator demo,
            IHttpClientGenerator live)
        {
            services.AddSingleton<IMtDataReaderClientsPair>(p => new MtDataReaderClientsPair(
                new MtDataReaderClient(demo),
                new MtDataReaderClient(live)));
        }

        [PublicAPI]
        public static void RegisterMtDataReaderClient(this IServiceCollection services, IHttpClientGenerator clientProxyGenerator)
        {
            services.AddSingleton<IMtDataReaderClient>(p => new MtDataReaderClient(clientProxyGenerator));
        }
    }
}