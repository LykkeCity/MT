using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Core
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LiquidationOperationState
    {
        Initiated = 0,
        Started = 1,
        Finished = 2,
        OnTheWayToFail = 3,
        Failed = 4,
    }
}