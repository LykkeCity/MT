// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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