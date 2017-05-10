using System;

namespace MarginTrading.Common.ClientContracts
{
    public class AccountHistoryItemClient
    {
        public DateTime Date { get; set; }
        public AccountHistoryClientContract Account { get; set; }
        public OrderHistoryClientContract Position { get; set; }
    }
}
