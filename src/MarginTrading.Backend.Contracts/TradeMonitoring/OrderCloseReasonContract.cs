using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderCloseReasonContract
    {
        None,
        Close,
        StopLoss,
        TakeProfit,
        StopOut,
        Canceled,
        CanceledBySystem,
        CanceledByBroker,
        ClosedByBroker
    }
}
