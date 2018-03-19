namespace MarginTrading.Backend.Contracts.Account
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