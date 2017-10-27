namespace MarginTrading.Core.MatchingEngines
{
    /// <summary>
    /// Routing direction set command for Trading Router from Risk Manager
    /// </summary>
    public class MatchingEngineRouteRisksCommand
    {
        /// <summary>
        /// Here id for required action is passed
        /// </summary>
        public RiskManagerActionType? ActionType { get; set; }
        
        /// <summary>
        /// ON / OFF
        /// </summary>
        public RiskManagerAction? Action { get; set; }
        
        /// <summary>
        /// Breached metric type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Matching engine Id for hedging
        /// </summary>
        public string MatchingEngineId { get; set; }

        /// <summary>
        /// Soft or Hard
        /// </summary>
        public string LimitType { get; set; }
        
        public int Rank { get; set; }

        public string ClientId { get; set; }

        public string Instrument { get; set; }

        public string Asset { get; set; }
        
        public RouteDirection? Direction { get; set; }
        
        public string TradingConditionId { get; set; }
    }

    public enum RiskManagerActionType
    {
        ExternalExchangePassThrough,
        BlockTradingForNewOrders
    }
    
    public enum RiskManagerAction
    {
        On,
        Off
    }

    public enum RouteDirection
    {
        Buy,
        Sell
    }
}