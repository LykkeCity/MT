using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderDirectionContract
    {
        Buy,
        Sell
    }
}
