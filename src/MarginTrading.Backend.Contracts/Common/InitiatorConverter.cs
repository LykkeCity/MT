// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Contracts.Common
{
    public class InitiatorConverter : JsonConverter<Initiator>
    {
        public override void WriteJson(JsonWriter writer, Initiator value, JsonSerializer serializer)
        {
            writer.WriteValue((string) value);
        }

        public override Initiator ReadJson(JsonReader reader,
            Type objectType,
            Initiator existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var source = (string)reader.Value;
            return (Initiator)source;
        }
    }
}