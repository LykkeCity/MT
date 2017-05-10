using System.Collections.Generic;

namespace MarginTrading.Common.ClientContracts
{
    public class InitDataLiveDemoClientResponse
    {
        public InitDataClientResponse Live { get; set; }
        public InitDataClientResponse Demo { get; set; }
    }

    public class InitDataClientResponse
    {
        public MarginTradingAccountClientContract[] Accounts { get; set; }
        public Dictionary<string, MarginTradingAssetClientContract[]> AccountAssetPairs { get; set; }
        public Dictionary<string, GraphBidAskPairClientContract[]> ChartData { get; set; }
    }
}
