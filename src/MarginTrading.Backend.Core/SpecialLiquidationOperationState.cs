using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Core
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SpecialLiquidationOperationState
    {
        Initiated = 0,
        Started = 1,
        PriceRequested = 2,
        PriceReceived = 3,
        ExternalOrderExecuted = 4,
        InternalOrderExecutionStarted = 5,
        InternalOrdersExecuted = 6,
        Finished = 7,
        OnTheWayToFail = 8,
        Failed = 9,
    }
}