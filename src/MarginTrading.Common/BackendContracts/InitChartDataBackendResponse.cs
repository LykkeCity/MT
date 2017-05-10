using System.Collections.Generic;
using System.Linq;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class InitChartDataBackendResponse
    {
        public Dictionary<string, GraphBidAskPairBackendContract[]> ChartData { get; set; }

        public static InitChartDataBackendResponse Create(Dictionary<string, List<GraphBidAskPair>> chartData)
        {
            return new InitChartDataBackendResponse
            {
                ChartData = chartData.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToBackendContract()).ToArray())
            };
        }
    }
}
