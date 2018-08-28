namespace MarginTrading.Backend.Core
{
    public enum SpecialLiquidationOperationState
    {
        Initiated = 0,
        Started = 1,
        PriceRequested = 2,
        PriceReceived = 3,
        OrderExecuted = 4,
        Finished = 5,
        Failed = 6,
    }
}