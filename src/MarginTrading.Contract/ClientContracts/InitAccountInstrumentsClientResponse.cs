using System.Collections.Generic;

namespace MarginTrading.Contract.ClientContracts
{
    public class InitAccountInstrumentsClientResponse
    {
        public Dictionary<string, AccountAssetPairClientContract[]> TradingConditions { get; set; }
    }
}
