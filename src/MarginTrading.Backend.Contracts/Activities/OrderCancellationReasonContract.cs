// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.Activities
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderCancellationReasonContract
    {
        None,
        BaseOrderCancelled,
        ParentPositionClosed,
        ConnectedOrderExecuted,
        CorporateAction,
        InstrumentInvalidated,
        AccountInactivated,
        Expired
    }
}