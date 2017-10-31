using System.Collections.Generic;

namespace MarginTrading.Contract.ClientContracts
{
    public class InitChartDataClientResponse
    {
        public Dictionary<string, GraphBidAskPairClientContract[]> ChartData { get; set; }
    }
}
