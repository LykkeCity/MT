﻿namespace MarginTrading.Contract.BackendContracts
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