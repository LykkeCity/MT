namespace MarginTrading.Backend.Core.TradingConditions
{
    public class TradingCondition : ITradingCondition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string MatchingEngineId { get; set; }
        public bool IsDefault { get; set; }

        public static TradingCondition Create(ITradingCondition src)
        {
            return new TradingCondition
            {
                Id = src.Id,
                Name = src.Name,
                MatchingEngineId = src.MatchingEngineId,
                IsDefault = src.IsDefault
            };
        }
    }
}