using System.Collections.Generic;
using System.Linq;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class InitAccountInstrumentsBackendResponse
    {
        public Dictionary<string, MarginTradingAccountAssetBackendContract[]> AccountAssets { get; set; }

        public static InitAccountInstrumentsBackendResponse Create(Dictionary<string, IMarginTradingAccountAsset[]> accountAssets)
        {
            return new InitAccountInstrumentsBackendResponse
            {
                AccountAssets = accountAssets.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToBackendContract()).ToArray())
            };
        }

        public static InitAccountInstrumentsBackendResponse CreateEmpty()
        {
            return new InitAccountInstrumentsBackendResponse
            {
                AccountAssets = new Dictionary<string, MarginTradingAccountAssetBackendContract[]>()
            };
        }
    }
}
