using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Contract.BackendContracts
{
    public class InitAccountInstrumentsBackendResponse
    {
        public Dictionary<string, AccountAssetPairBackendContract[]> AccountAssets { get; set; }

        public static InitAccountInstrumentsBackendResponse CreateEmpty()
        {
            return new InitAccountInstrumentsBackendResponse
            {
                AccountAssets = new Dictionary<string, AccountAssetPairBackendContract[]>()
            };
        }
    }
}
