using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core.Helpers
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Returns value for specified <paramref name="key"/> from the <paramref name="dictionary"/>. <br/>
        /// If the value is not found - the result of <paramref name="defaultValueFactory"/> is returned. <br/>
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
            Func<TKey, TValue> defaultValueFactory)
        {
            return dictionary.TryGetValue(key, out var value)
                ? value
                : defaultValueFactory(key);
        }

        /// <summary>
        /// Returns value for specified <paramref name="key"/> from the <paramref name="dictionary"/>. <br/>
        /// If the value is not found - then default(<typeparamref name="TValue"/>) is returned. <br/>
        /// </summary>
        [CanBeNull]
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out var value)
                ? value
                : default(TValue);
        }
    }
}
