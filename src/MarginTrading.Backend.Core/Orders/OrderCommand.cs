// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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