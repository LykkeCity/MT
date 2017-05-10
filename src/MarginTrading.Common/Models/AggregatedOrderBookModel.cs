using System.Collections.Generic;
using MarginTrading.Core;

namespace MarginTrading.Common.Models
{
    public class AggregatedOrderBookModel
    {
        public List<AggregatedOrderBookItem> Buy { get; set; } = new List<AggregatedOrderBookItem>();
        public List<AggregatedOrderBookItem> Sell { get; set; } = new List<AggregatedOrderBookItem>();
    }
}