using System.Collections.Generic;

namespace MarginTrading.Contract.ClientContracts
{
    public class InitDataClientResponse
    {
        public MarginTradingAccountClientContract[] Accounts { get; set; }
        public Dictionary<string, AccountAssetPairClientContract[]> TradingConditions { get; set; }
    }
}
