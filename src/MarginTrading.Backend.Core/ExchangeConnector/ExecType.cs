// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.ExchangeConnector
{
    public enum ExecType
    {
        Unknown,
        New,
        PartialFill,
        Fill,
        DoneForDay,
        Cancelled,
        Replace,
        PendingCancel,
        Stopped,
        Rejected,
        Suspended,
        PendingNew,
        Calculated,
        Expired,
        Restarted,
        PendingReplace,
        Trade,
        TradeCorrect,
        TradeCancel,
        OrderStatus
    }
}