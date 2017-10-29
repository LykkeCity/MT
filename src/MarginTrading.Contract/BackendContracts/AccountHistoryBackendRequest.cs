using System;

namespace MarginTrading.Contract.BackendContracts
{
    public class AccountHistoryBackendRequest
    {
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
