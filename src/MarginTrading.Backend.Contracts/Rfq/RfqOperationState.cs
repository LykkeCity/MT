// Copyright (c) 2021 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.Rfq
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RfqOperationState
    {
        Initiated,
        Started,
        PriceRequested,
        PriceReceived,
        ExternalOrderExecuted,
        InternalOrderExecutionStarted,
        InternalOrdersExecuted,
        Finished,
        OnTheWayToFail,
        Failed
    }
}