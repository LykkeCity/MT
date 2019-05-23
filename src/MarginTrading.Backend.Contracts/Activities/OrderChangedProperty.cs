using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.Activities
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderChangedProperty
    {
        None = 0,
        Price = 1,
        Volume = 2,
        RelatedOrderRemoved = 3,
        Validity = 4,
        ForceOpen = 5,
    }
}