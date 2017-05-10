using System.Collections.Generic;

namespace MarginTrading.Common.ClientContracts
{
    public class InitChartDataLiveDemoClientResponse
    {
        public InitChartDataClientResponse Live { get; set; }
        public InitChartDataClientResponse Demo { get; set; }
    }

    public class InitChartDataClientResponse
    {
        public Dictionary<string, GraphBidAskPairClientContract[]> ChartData { get; set; }
    }
}
