// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.Activities
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderChangedProperty
    {
        None = 0,
        Price = 1,
        Volume = 2,
        RelatedOrderRemoved = 3,
        Validity = 4,
        ForceOpen = 5,
    }
}