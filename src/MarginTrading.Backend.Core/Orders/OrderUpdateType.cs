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