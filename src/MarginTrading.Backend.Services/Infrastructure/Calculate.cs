// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Services.Infrastructure
{
    /// <summary>
    ///     Represents a lazy function whose value will not be recalculated if source value is not changed.<br />
    ///     Caches last calculation execution result.<br />
    ///     Source function is executed on each act of obtaining the calculation result.
    /// </summary>
    public class Calculate
    {
        public static ICachedCalculation<TResult> Cached<TSource, TResult>(Func<TSource> source,
            Func<TSource, TSource, bool> compareSources, Func<TSource, TResult> calculation)
        {
            return new CachedCalculation<TSource, TResult>(source, compareSources, calculation);
        }

        /// <summary>
        ///     Represents a lazy function whose value will not be recalculated if source value is not changed.<br />
        ///     Caches last calculation execution result.<br />
        ///     Source function is executed on each act of obtaining the calculation result. It should represent a rarely
        ///     changeable value.<br />
        ///     Instances are not very thread-safe, but at least it is always guaranteed not to mix up the results for different
        ///     source values.
        /// </summary>
        private class CachedCalculation<TSource, TResult> : ICachedCalculation<TResult>
        {
            private readonly Func<TSource> _source;
            private readonly Func<TSource, TSource, bool> _compareSources;
            private readonly Func<TSource, TResult> _calculation;

            [CanBeNull] private volatile SourceResultPair _cache;

            public CachedCalculation(Func<TSource> source, Func<TSource, TSource, bool> compareSources,
                Func<TSource, TResult> calculation)
            {
                _source = source;
                _compareSources = compareSources;
                _calculation = calculation;
            }

            public TResult Get()
            {
                var cached = _cache;
                var source = _source();
                if (cached != null && _compareSources(cached.Source, source))
                {
                    return cached.Result;
                }

                var newCache = new SourceResultPair(source, _calculation(source));
                _cache = newCache; // it could have changed from other thread
                return newCache.Result;
            }

            private class SourceResultPair
            {
                public SourceResultPair(TSource source, TResult result)
                {
                    Source = source;
                    Result = result;
                }

                public TSource Source { get; }
                public TResult Result { get; }
            }
        }
    }
}