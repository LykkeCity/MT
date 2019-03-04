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