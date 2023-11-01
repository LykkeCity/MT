// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarginTrading.Common.Extensions
{
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Traverses the object graph and returns all properties of the specified type.
        /// Doesn't traverse arrays, doesn't return null properties.
        /// Analyzes only public instance properties which are classes or structs.
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> GetPropertiesOfType<T>(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            
            if (obj.GetType().IsArray)
                yield break;

            var targetProperties = obj.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.PropertyType.IsClass || p.PropertyType.IsUserDefinedStruct());

            var targetType = typeof(T);
            foreach (var property in targetProperties)
            {
                if (property.GetValue(obj) == null)
                    continue;
                
                if (property.PropertyType == targetType)
                {
                    yield return (T)property.GetValue(obj);
                    continue;
                }

                if (targetType.IsAssignableFrom(property.PropertyType))
                {
                    yield return (T)property.GetValue(obj);
                    continue;
                }

                var childProperties = property.GetValue(obj).GetPropertiesOfType<T>();
                foreach (var childProperty in childProperties)
                {
                    yield return childProperty;
                }
            }
        }

        private static bool IsUserDefinedStruct(this Type type)
        {
            return type is { IsValueType: true, IsPrimitive: false };
        }
    }
}