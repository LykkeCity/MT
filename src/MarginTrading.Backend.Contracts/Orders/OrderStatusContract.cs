// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Backend.Contracts.Orders
{
    public enum OrderStatusContract
    {
        Placed = 0,
        Inactive = 1,
        Active = 2,
        ExecutionStarted = 3,
        Executed = 4,
        Canceled = 5,
        Rejected = 6,
        Expired = 7
    }
}