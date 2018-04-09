using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Polly;
using Polly.Caching;
using Polly.Caching.MemoryCache;
using Refit;

namespace MarginTrading.Backend.Contracts.Infrastructure
{
    internal class CachingHelper
    {
        [CanBeNull] private readonly Func<MethodInfo, object[], TimeSpan, TimeSpan> _cachingDurationProvider;
        private readonly IAsyncPolicy _retryPolicy;
        
        public CachingHelper([CanBeNull] Func<MethodInfo, object[], TimeSpan, TimeSpan> cachingDurationProvider)
        {
            _cachingDurationProvider = cachingDurationProvider;
            _retryPolicy = Policy
                .CacheAsync(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())),
                    new ContextualTtl());
        }

        public Task<object> HandleMethodCall(MethodInfo targetMethod, object[] args, Func<Task<object>> innerHandler)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable once HeuristicUnreachableCode
            if (targetMethod.GetCustomAttribute<GetAttribute>() == null)
                return innerHandler();

            var clientCachingAttribute = targetMethod.GetCustomAttribute<ClientCachingAttribute>();
            var attributeCachingTime = clientCachingAttribute?.CachingTime ?? TimeSpan.Zero;
            attributeCachingTime = attributeCachingTime < TimeSpan.Zero ? TimeSpan.Zero : attributeCachingTime;
            var cachingTime = (_cachingDurationProvider ?? ((mi, a, t) => t))
                .Invoke(targetMethod, args, attributeCachingTime);
            
            var contextData = new Dictionary<string, object>
            {
                {ContextualTtl.TimeSpanKey, cachingTime}
            };
            return _retryPolicy.ExecuteAsync((context, ct) => innerHandler(),
                new Context($"{targetMethod.DeclaringType}:{targetMethod.Name}:{targetMethod.GetHashCode()}:{JsonConvert.SerializeObject(args)}",
                    contextData), default);
        }
    }
}