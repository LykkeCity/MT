// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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