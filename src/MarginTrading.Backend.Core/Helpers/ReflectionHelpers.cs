using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core.Helpers
{
    public static class ReflectionHelpers
    {
        /// <summary>
        /// For order/position merging on initialization only!
        /// Return true if there was difference, false if items were the same.
        /// </summary>
        public static bool SetIfDiffer<T>(this T obj, Dictionary<string, object> propertyData)
            where T: class
        {
            var properties = obj.GetType().GetProperties()
                .Where(x => Attribute.IsDefined(x, typeof(JsonPropertyAttribute)))
                .ToDictionary(x => x.Name, x => x);

            var result = false;
            foreach (var data in propertyData)
            {
                if (!properties.TryGetValue(data.Key, out var property) 
                    || (property.PropertyType.IsValueType && property.GetValue(obj) == data.Value)
                    || (!property.PropertyType.IsValueType && property.GetValue(obj).ToJson() == data.Value.ToJson()))// kind of a hack
                {
                    continue;
                }

                property.SetValue(obj, data.Value);
                result = true;
            }
            
            return result;
        }
    }
}