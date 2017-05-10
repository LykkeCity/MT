using MarginTrading.Core;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Services.Events
{
    public class LimitOrderSetEventArgs
    {
        public IEnumerable<LimitOrder> PlacedLimitOrders { get; set; } = new List<LimitOrder>();
        public IEnumerable<LimitOrder> DeletedLimitOrders { get; set; } = new List<LimitOrder>();

        public bool HasEvents()
        {
            return PlacedLimitOrders.Count() > 0 || DeletedLimitOrders.Count() > 0;
        }
    }
}
