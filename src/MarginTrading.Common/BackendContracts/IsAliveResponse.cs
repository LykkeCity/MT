namespace MarginTrading.Common.BackendContracts
{
    public class IsAliveResponse
    {
        public bool MatchingEngineAlive { get; set; }
        public bool TradingEngineAlive { get; set; }
        public string Version { get; set; }
        public string Env { get; set; }
    }
}
