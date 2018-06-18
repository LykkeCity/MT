namespace MarginTrading.Backend.Core.Orders
{
    public enum OrderUpdateType
    {
        Place,
        Cancel,
        Activate,
        Reject,
        Closing,
        Close,
        ChangeOrderLimits,
    }
}