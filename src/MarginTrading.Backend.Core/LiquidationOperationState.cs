using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Core
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LiquidationOperationState
    {
        Initiated = 0,
        Started = 1,
        SpecialLiquidationStarted = 2,
        Finished = 3,
        Failed = 4,
    }
}