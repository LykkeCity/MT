// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Backend.Core.Orders
{
    public enum OrderUpdateType
    {
        Place,
        Activate,
        Change,
        Cancel,
        Reject,
        ExecutionStarted,
        Executed
    }
}