namespace MarginTrading.Backend.Contracts.Events
{
    public enum OrderHistoryTypeContract
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