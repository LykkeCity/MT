using System.Collections.Generic;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Models
{
    public class OrderBookModel
    {
        public string Instrument { get; set; }
        public List<LimitOrder> Buy { get; set; } = new List<LimitOrder>();
        public List<LimitOrder> Sell { get; set; } = new List<LimitOrder>();
    }
}
