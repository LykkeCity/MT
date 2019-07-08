// Copyright (c) 2019 Lykke Corp.

using System.Collections.Generic;
using MarginTrading.Contract.BackendContracts.TradingConditions;

namespace MarginTrading.Contract.BackendContracts
{
    public class InitAccountInstrumentsBackendResponse
    {
        public Dictionary<string, AccountAssetPairModel[]> AccountAssets { get; set; }

        public static InitAccountInstrumentsBackendResponse CreateEmpty()
        {
            return new InitAccountInstrumentsBackendResponse
            {
                AccountAssets = new Dictionary<string, AccountAssetPairModel[]>()
            };
        }
    }
}
