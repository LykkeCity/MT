using System;

namespace MarginTrading.Common.ClientContracts
{
    public class BidAskClientContract
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
    }
}
