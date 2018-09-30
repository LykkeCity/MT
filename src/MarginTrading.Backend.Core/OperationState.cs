using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Core
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OperationState
    {
        Initiated = 0,
        Started = 1,
        Finished = 2,
    }
}