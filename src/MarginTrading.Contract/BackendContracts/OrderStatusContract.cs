// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.BackendContracts
{
    public enum OrderStatusContract
    {
        WaitingForExecution,
        Active,
        Closed,
        Rejected,
        Closing
    }
}