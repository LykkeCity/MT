using System.Collections.Generic;

namespace MarginTrading.Common.ClientContracts
{
    public class InitAccountInstrumentsLiveDemoClientResponse
    {
        public InitAccountInstrumentsClientResponse Live { get; set; }
        public InitAccountInstrumentsClientResponse Demo { get; set; }
    }

    public class InitAccountInstrumentsClientResponse
    {
        public Dictionary<string, MarginTradingAccountAssetClientContract[]> TradingConditions { get; set; }
    }
}
