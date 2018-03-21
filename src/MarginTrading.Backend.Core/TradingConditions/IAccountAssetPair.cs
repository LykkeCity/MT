namespace MarginTrading.Backend.Core.TradingConditions
{
    public interface IAccountAssetPair
    {
        string TradingConditionId { get; }
        string BaseAssetId { get; }
        string Instrument { get; }
        int LeverageInit { get; }
        int LeverageMaintenance { get; }
        decimal SwapLong { get; }
        decimal SwapShort { get; }
        decimal OvernightSwapLong { get; }
        decimal OvernightSwapShort { get; }
        decimal CommissionLong { get; }
        decimal CommissionShort { get; }
        decimal CommissionLot { get; }
        decimal DeltaBid { get; }
        decimal DeltaAsk { get; }
        decimal DealLimit { get; }
        decimal PositionLimit { get; }
    }
}
