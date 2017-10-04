using System;

namespace MarginTrading.Common.ClientContracts
{
    public class BidAskClientContract
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
    }
}
