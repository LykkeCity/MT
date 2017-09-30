using System;

namespace MarginTrading.Common.ClientContracts
{
    public class InitPricesResponse
    {
        public DateTime ServerTime { get; set; }
        public BidAskClientContract[] Prices { get; set; }
    }
}
