using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.Backend.Contracts.Client
{
    public static class ServiceCollectionExtensions
    {
        [PublicAPI]
        public static void RegisterMtBackendClientsPair(this IServiceCollection services, string demoUrl, string liveUrl, string demoKey, string liveKey, string userAgent)
        {
            services.AddSingleton<IMtBackendClientsPair>(p => new MtBackendClientsPair(
                new MtBackendClient(demoUrl, demoKey, userAgent), 
                new MtBackendClient(liveUrl, liveKey, userAgent)));
        }
        
        [PublicAPI]
        public static void RegisterMtBackendClient(this IServiceCollection services, string url, string key, string userAgent)
        {
            services.AddSingleton<IMtBackendClient>(p => new MtBackendClient(url, key, userAgent));
        }
    }
}