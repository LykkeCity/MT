using System;

namespace MarginTrading.Backend.Contracts.Account
{
    public class AccountHistoryItemBackend
    {
        public DateTime Date { get; set; }
        public AccountHistoryBackendContract Account { get; set; }
        public OrderHistoryBackendContract Position { get; set; }
    }
}
