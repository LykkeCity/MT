using System;

namespace MarginTrading.Contract.BackendContracts
{
    public class IsAliveResponse
    {
        public bool MatchingEngineAlive { get; set; }
        public bool TradingEngineAlive { get; set; }
        public string Version { get; set; }
        public string Env { get; set; }
        public DateTime ServerTime { get; set; }
    }
}
