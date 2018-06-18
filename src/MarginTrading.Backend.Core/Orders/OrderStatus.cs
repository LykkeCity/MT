namespace MarginTrading.Backend.Core.Orders
{
    public enum OrderStatus
    {
        WaitingForExecution,
        Active,
        Closed,
        Rejected,
        Closing
    }
}