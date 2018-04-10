using JetBrains.Annotations;
using Lykke.ClientGenerator;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    public static class ServiceCollectionExtensions
    {
        [PublicAPI]
        public static void RegisterMtDataReaderClientsPair(this IServiceCollection services, IClientProxyGenerator demo,
            IClientProxyGenerator live)
        {
            services.AddSingleton<IMtDataReaderClientsPair>(p => new MtDataReaderClientsPair(
                new MtDataReaderClient(demo),
                new MtDataReaderClient(live)));
        }

        [PublicAPI]
        public static void RegisterMtDataReaderClient(this IServiceCollection services, IClientProxyGenerator clientProxyGenerator)
        {
            services.AddSingleton<IMtDataReaderClient>(p => new MtDataReaderClient(clientProxyGenerator));
        }
    }
}