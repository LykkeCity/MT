using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Polly;
using Polly.Caching;
using Polly.Caching.MemoryCache;
using Refit;

namespace MarginTrading.Backend.Contracts.Infrastructure
{
    public class CachingHelper
    {
        private readonly IAsyncPolicy _retryPolicy;

        public CachingHelper()
        {
            _retryPolicy = Policy
                .CacheAsync(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())),
                    new ContextualTtl());
        }

        public Task<object> HandleMethodCall(MethodInfo targetMethod, object[] args, Func<Task<object>> innerHandler)
        {
            var clientCachingAttribute = targetMethod.GetCustomAttribute<ClientCachingAttribute>();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable once HeuristicUnreachableCode
            if (clientCachingAttribute == null) return innerHandler();

            if (clientCachingAttribute.CachingTime <= TimeSpan.Zero)
            {
                throw new InvalidOperationException(
                    $"Method {targetMethod.DeclaringType}.{targetMethod.Name} has {nameof(ClientCachingAttribute)} " +
                    "specified, but it has invalid caching time specified");
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode
            if (targetMethod.GetCustomAttribute<GetAttribute>() == null)
            {
                throw new InvalidOperationException(
                    $"Method {targetMethod.DeclaringType}.{targetMethod.Name} has {nameof(ClientCachingAttribute)} " +
                    "specified, but it is not a Refit GET method");
            }
            // ReSharper restore HeuristicUnreachableCode

            var contextData = new Dictionary<string, object>
            {
                {ContextualTtl.TimeSpanKey, clientCachingAttribute.CachingTime}
            };
            return _retryPolicy.ExecuteAsync((context, ct) => innerHandler(),
                new Context($"{targetMethod.DeclaringType}:{targetMethod.Name}:{targetMethod.GetHashCode()}:{JsonConvert.SerializeObject(args)}",
                    contextData), default);
        }
    }
}