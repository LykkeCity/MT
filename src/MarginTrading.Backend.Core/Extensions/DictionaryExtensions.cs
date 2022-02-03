// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class DictionaryExtensions
    {
        public static  IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> src, IDictionary<TKey, TValue> merge)
        {
            if (merge == null || !merge.Any())
                return src;
            
            foreach (var (key, value) in merge)
            {
                src[key] = value;
            }

            return src;
        }
    }
}