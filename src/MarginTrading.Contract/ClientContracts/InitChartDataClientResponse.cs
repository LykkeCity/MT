// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MarginTrading.Contract.ClientContracts
{
    public class InitChartDataClientResponse
    {
        public Dictionary<string, GraphBidAskPairClientContract[]> ChartData { get; set; }
    }
}
