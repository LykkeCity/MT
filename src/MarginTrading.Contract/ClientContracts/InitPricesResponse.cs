using System;

namespace MarginTrading.Contract.ClientContracts
{
    public class InitPricesResponse
    {
        public DateTime ServerTime { get; set; }
        public BidAskClientContract[] Prices { get; set; }
    }
}
