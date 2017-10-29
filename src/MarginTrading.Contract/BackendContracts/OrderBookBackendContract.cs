using System.Collections.Generic;

namespace MarginTrading.Contract.BackendContracts
{
    public class OrderBookBackendContract
    {
        public Dictionary<decimal, LimitOrderBackendContract[]> Buy { get; set; }
        public Dictionary<decimal, LimitOrderBackendContract[]> Sell { get; set; }
    }
}
