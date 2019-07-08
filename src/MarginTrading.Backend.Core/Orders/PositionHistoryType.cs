// Copyright (c) 2019 Lykke Corp.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Core.Orders
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PositionHistoryType
    {
        Open,
        PartiallyClose,
        Close
    }
}