using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.Common.Json;
using StackExchange.Redis;

namespace MarginTrading.Backend.Core.Mappers
{
    [UsedImplicitly]
    public static class RedisSerializationExtensions
    {
        public static HashEntry[] Serialize(this IEnumerable<Order> orders, Func<Order, string> keySelector)
        {
            return orders.Select(x => new HashEntry(keySelector(x), CacheSerializer.Serialize(x))).ToArray();
        }

        public static RedisValue Serialize(this Order order)
        {
            return CacheSerializer.Serialize(order);
        }

        public static RedisValue[] Serialize(this HashSet<string> dictionary)
        {
            return dictionary.Select(x => (RedisValue)CacheSerializer.Serialize(x)).ToArray();
        }

        public static bool TryDeserialize(this RedisValue data, out Order order)
        {
            order = null;
            if (data.IsNullOrEmpty)
            {
                return false;
            }

            var deserialized = CacheSerializer.Deserialize<Order>(data);
            
            if (deserialized == null)
            {
                return false;
            }

            order = deserialized;
            return true;
        }

        public static Order Deserialize(this HashEntry hashEntry)
        {
            return CacheSerializer.Deserialize<Order>(hashEntry.Value);
        }
    }
}