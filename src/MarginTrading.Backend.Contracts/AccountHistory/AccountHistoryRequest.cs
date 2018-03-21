using System;

namespace MarginTrading.Backend.Contracts.AccountHistory
{
    public class AccountHistoryRequest
    {
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}