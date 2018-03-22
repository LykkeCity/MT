namespace MarginTrading.Backend.Contracts.TradingConditions
{
    public class TradingConditionContract
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public string LegalEntity { get; set; }
    }
}
