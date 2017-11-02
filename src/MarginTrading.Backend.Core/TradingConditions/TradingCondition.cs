namespace MarginTrading.Backend.Core.TradingConditions
{
    public class TradingCondition : ITradingCondition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }

        public static TradingCondition Create(ITradingCondition src)
        {
            return new TradingCondition
            {
                Id = src.Id,
                Name = src.Name,
                IsDefault = src.IsDefault
            };
        }
    }
}