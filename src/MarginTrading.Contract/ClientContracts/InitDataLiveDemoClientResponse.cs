// Copyright (c) 2019 Lykke Corp.

using System.Collections.Generic;

namespace MarginTrading.Contract.ClientContracts
{
    public class InitDataLiveDemoClientResponse
    {
        public InitDataClientResponse Live { get; set; }
        public InitDataClientResponse Demo { get; set; }
        public AssetPairClientContract[] Assets { get; set; }
        public Dictionary<string, BidAskClientContract> Prices { get; set; }
    }
}