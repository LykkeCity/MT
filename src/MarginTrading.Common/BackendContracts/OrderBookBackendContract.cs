using System.Collections.Generic;

namespace MarginTrading.Common.BackendContracts
{
    public class OrderBookBackendContract
    {
        public Dictionary<double, LimitOrderBackendContract[]> Buy { get; set; }
        public Dictionary<double, LimitOrderBackendContract[]> Sell { get; set; }
    }
}
