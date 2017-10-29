namespace MarginTrading.Contract.BackendContracts
{
    public class NewMatchingEngineRouteRequest 
    {        
        public int Rank { get; set; }
        public string TradingConditionId { get; set; }
        public string ClientId { get; set; }
        public string Instrument { get; set; }
        public OrderDirectionContract? Type { get; set; }
        public string MatchingEngineId { get; set; }
        public string Asset { get; set; }
    }
}
