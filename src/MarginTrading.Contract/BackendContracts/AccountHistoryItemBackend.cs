using System;

namespace MarginTrading.Contract.BackendContracts
{
    public class AccountHistoryItemBackend
    {
        public DateTime Date { get; set; }
        public AccountHistoryBackendContract Account { get; set; }
        public OrderHistoryBackendContract Position { get; set; }
    }
}
