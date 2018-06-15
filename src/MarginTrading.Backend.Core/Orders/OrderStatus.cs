namespace MarginTrading.Backend.Core.Orders
{
    public enum OrderStatus
    {
        Created = 0,
        Inactive = 1,
        Active = 2,
        ExecutionStarted = 3,
        Executed = 4,
        Canceled = 5,
        Rejected = 6,
        Expired = 7
    }
}