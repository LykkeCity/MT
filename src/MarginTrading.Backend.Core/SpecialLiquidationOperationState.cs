namespace MarginTrading.Backend.Core
{
    public enum SpecialLiquidationOperationState
    {
        Initiated = 0,
        Started = 1,
        PriceRequested = 2,
        PriceReceived = 3,
        ExternalOrderExecuted = 4,
        InternalOrdersExecuted = 5,
        Finished = 6,
        OnTheWayToFail = 7,
        Failed = 8,
    }
}