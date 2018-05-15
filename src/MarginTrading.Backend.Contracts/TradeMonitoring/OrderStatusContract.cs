using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderStatusContract
    {
        WaitingForExecution,
        Active,
        Closed,
        Rejected,
        Closing
    }
}
