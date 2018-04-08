using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    public static class ServiceCollectionExtensions
    {
        [PublicAPI]
        public static void RegisterMtDataReaderClientsPair(this IServiceCollection services, string demoUrl,
            string liveUrl, string demoKey, string liveKey, string userAgent)
        {
            services.AddSingleton<IMtDataReaderClientsPair>(p => new MtDataReaderClientsPair(
                new MtDataReaderClient(demoUrl, demoKey, userAgent),
                new MtDataReaderClient(liveUrl, liveKey, userAgent)));
        }

        [PublicAPI]
        public static void RegisterMtDataReaderClient(this IServiceCollection services, string url, string key,
            string userAgent)
        {
            services.AddSingleton<IMtDataReaderClient>(p => new MtDataReaderClient(url, key, userAgent));
        }
    }
}