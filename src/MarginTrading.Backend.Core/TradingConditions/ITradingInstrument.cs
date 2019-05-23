namespace MarginTrading.Backend.Core.TradingConditions
{
    public interface ITradingInstrument
    {
        string TradingConditionId { get; }
        string Instrument { get; }
        int LeverageInit { get; }
        int LeverageMaintenance { get; }
        decimal SwapLong { get; }
        decimal SwapShort { get; }
        
        decimal Delta { get; }
        decimal DealMinLimit { get; }
        decimal DealMaxLimit { get; }
        decimal PositionLimit { get; }
        bool ShortPosition { get; }
        decimal LiquidationThreshold { get; }
        decimal OvernightMarginMultiplier { get; }
        
        decimal CommissionRate { get; }
        decimal CommissionMin { get; }
        decimal CommissionMax { get; }
        string CommissionCurrency { get; }
    }
}
