namespace MarginTrading.Contract.BackendContracts.TradingConditions
{
    public class AssignInstrumentsRequest
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public string[] Instruments { get; set; }
    }
}