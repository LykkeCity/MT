// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Common.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> @this)
            => @this == null || !@this.Any();

        /// <summary>
        /// Fixing the order of elements in the list, produces <see cref="SortedList{TKey,TValue}"/>
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static SortedList<int, T> FreezeOrder<T>(this IEnumerable<T> source) =>
            new SortedList<int, T>(source
                .Select((e, i) => new {Element = e, Rank = i})
                .ToDictionary(m => m.Rank, m => m.Element));
        
        /// <summary>
        /// Converts any <see cref="IEnumerable{T}"/> into <see cref="SortedList{TKey,TValue}"/> using key selector
        /// and element selector delegates 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <param name="valueSelector"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public static SortedList<TKey, TValue> ToSortedList<TSource, TKey, TValue>(this IEnumerable<TSource> source, 
            Func<TSource, TKey> keySelector, 
            Func<TSource, TValue> valueSelector)
        {
            var result = new SortedList<TKey, TValue>();
            
            foreach (var element in source)
            {
                result.Add(keySelector(element), valueSelector(element));
            }
            
            return result;
        }
    }
}