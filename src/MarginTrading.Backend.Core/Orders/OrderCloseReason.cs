// Copyright (c) 2019 Lykke Corp.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Core.Orders
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderCloseReason
    {
        
        None,
        Close,
        StopLoss,
        TakeProfit,
        StopOut,
        Canceled,
        CanceledBySystem,
        CanceledByBroker,
        ClosedByBroker,
    }
}