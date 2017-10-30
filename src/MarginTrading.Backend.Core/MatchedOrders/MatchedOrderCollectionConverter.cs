using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core.MatchedOrders
{
    public class MatchedOrderCollectionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MatchedOrderCollection);
        }

        public override object ReadJson(
            JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer)
        {
            var list = reader != null ? serializer.Deserialize<List<MatchedOrder>>(reader) : null;
            return new MatchedOrderCollection(list);
        }

        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer)
        {
            var collection = (MatchedOrderCollection) value;
            serializer.Serialize(writer, collection.Items);
        }
    }
}