// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Backend.Core.Orders
{
    public enum OrderCommand
    {
        MakeInactive = 0,
        Activate = 1,
        StartExecution = 2,
        FinishExecution = 3,
        Reject = 4,
        Cancel = 5,
        Expire = 6,
        CancelExecution = 7,
    }
}