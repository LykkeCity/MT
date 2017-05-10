using System;

namespace MarginTrading.Core
{
    public interface IElementaryTransaction
    {
        string CounterPartyId { get; set; }
        string AccountId { get; set; }
        string Asset { get; set; }
        double? Amount { get; set; }
        string TradingTransactionId { get; set; }
        double? AmountInUsd { get; set; }
        DateTime? TimeStamp { get; set; }
    }
}