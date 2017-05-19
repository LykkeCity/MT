using System.Collections.Generic;

namespace MarginTrading.Common.ClientContracts
{
    public class InitDataLiveDemoClientResponse
    {
        public InitDataClientResponse Live { get; set; }
        public InitDataClientResponse Demo { get; set; }
        public MarginTradingAssetClientContract[] Assets { get; set; }
    }

    public class InitDataClientResponse
    {
        public MarginTradingAccountClientContract[] Accounts { get; set; }
        public Dictionary<string, MarginTradingAccountAssetClientContract[]> TradingConditions { get; set; }
    }
}
