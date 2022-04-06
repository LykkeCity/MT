// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Core.Rfq
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PauseCancellationSource
    {
        Manual = 0,
        PriceReceived,
        TradingEnabled,
    }
}