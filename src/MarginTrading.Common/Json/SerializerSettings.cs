// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Common.Json
{
    public static class SerializerSettings
    {
        public static JsonConverter[] GetDefaultConverters()
        {
            return new JsonConverter[]
            {
                new IsoDateTimeConverter {DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'"}
            };
        }
    }
}
