using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Contract.BackendContracts
{
    public class InitChartDataBackendResponse
    {
        public Dictionary<string, GraphBidAskPairBackendContract[]> ChartData { get; set; }
    }
}
