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
