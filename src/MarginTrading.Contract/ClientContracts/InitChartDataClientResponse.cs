// Copyright (c) 2019 Lykke Corp.

using System.Collections.Generic;

namespace MarginTrading.Contract.ClientContracts
{
    public class InitChartDataClientResponse
    {
        public Dictionary<string, GraphBidAskPairClientContract[]> ChartData { get; set; }
    }
}
