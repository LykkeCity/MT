using System.Collections.Generic;

namespace MarginTrading.Common.ClientContracts
{
    public class InitChartDataClientResponse
    {
        public Dictionary<string, GraphBidAskPairClientContract[]> ChartData { get; set; }
    }
}
