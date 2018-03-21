namespace MarginTrading.Backend.Contracts.TradingConditions
{
    public class AssignInstrumentsContract
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public string[] Instruments { get; set; }
    }
}
