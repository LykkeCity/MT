using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.Events
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MarginEventTypeContract
    {
        MarginCall1,
        MarginCall2,
        OvernightMarginCall,
        Stopout,
    }
}