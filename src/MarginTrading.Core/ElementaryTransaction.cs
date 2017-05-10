using System;

namespace MarginTrading.Core
{
    public class ElementaryTransaction : IElementaryTransaction
    {
        public string CounterPartyId { get; set; }
        public string AccountId { get; set; }
        public string Asset { get; set; }
        public double? Amount { get; set; }
        public string TradingTransactionId { get; set; }
        public double? AmountInUsd { get; set; }
        public DateTime? TimeStamp { get; set; }
    }
}