using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Core.MatchingEngines
{
    public class SetOrderModel
    {
        public string MarketMakerId { get; set; }
        public bool DeleteAllBuy { get; set; }
        public bool DeleteAllSell { get; set; }
        [CanBeNull]
        public IReadOnlyList<string> DeleteByInstrumentsBuy { get; set; }
        [CanBeNull]
        public IReadOnlyList<string> DeleteByInstrumentsSell { get; set; }
        [CanBeNull]
        public IReadOnlyList<LimitOrder> OrdersToAdd { get; set; }
        public string[] OrderIdsToDelete { get; set; }
    }
}
