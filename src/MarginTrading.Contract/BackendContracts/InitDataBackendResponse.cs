using System;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Contract.BackendContracts
{
    public class InitDataBackendResponse
    {
        public MarginTradingAccountBackendContract[] Accounts { get; set; }
        public Dictionary<string, AccountAssetPairBackendContract[]> AccountAssetPairs { get; set; }
        public bool IsLive { get; set; }

        public static InitDataBackendResponse CreateEmpty()
        {
            return new InitDataBackendResponse
            {
                Accounts = Array.Empty<MarginTradingAccountBackendContract>(),
                AccountAssetPairs = new Dictionary<string, AccountAssetPairBackendContract[]>(),
            };
        }
    }
}
