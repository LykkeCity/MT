// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.Events
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MarginEventTypeContract
    {
        MarginCall1,
        MarginCall2,
        OvernightMarginCall,
        Stopout,
    }
}