using System.Collections.Generic;

namespace MarginTrading.Common.ClientContracts
{
    public class InitDataLiveDemoClientResponse
    {
        public InitDataClientResponse Live { get; set; }
        public InitDataClientResponse Demo { get; set; }
        public AssetPairClientContract[] Assets { get; set; }
        public Dictionary<string, BidAskClientContract> Prices { get; set; }
    }

    public class InitDataClientResponse
    {
        public MarginTradingAccountClientContract[] Accounts { get; set; }
        public Dictionary<string, AccountAssetPairClientContract[]> TradingConditions { get; set; }
    }
}
