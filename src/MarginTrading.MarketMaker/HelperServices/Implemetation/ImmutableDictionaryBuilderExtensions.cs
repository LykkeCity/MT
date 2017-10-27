using System.Collections.Generic;
using System.Collections.Immutable;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    public static class ImmutableDictionaryBuilderExtensions
    {
        /// <summary>
        /// Sets specified <paramref name="value"/> for all <paramref name="keys"/> in this <paramref name="builder"/>.
        /// </summary>
        public static ImmutableDictionary<TKey, TValue>.Builder SetValueForKeys<TKey, TValue>(
            this ImmutableDictionary<TKey, TValue>.Builder builder,
            IEnumerable<TKey> keys,
            TValue value)
        {
            foreach (var key in keys)
            {
                builder[key] = value;
            }

            return builder;
        }
    }
}