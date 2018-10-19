using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Core.Orders
{
    [JsonConverter(typeof (StringEnumConverter))]
    public enum OrderModality
    {
        [EnumMember(Value = "Unspecified")] Unspecified = 0,
        [EnumMember(Value = "Liquidation")] Liquidation = 76, // 0x4C
        [EnumMember(Value = "Regular")] Regular = 82, // 0x52
    }
}