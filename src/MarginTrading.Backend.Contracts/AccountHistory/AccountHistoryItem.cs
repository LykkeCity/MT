using System;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.AccountHistory
{
    public class AccountHistoryItem
    {
        public DateTime Date { get; set; }
        [CanBeNull] public AccountHistoryContract Account { get; set; }
        [CanBeNull] public OrderHistoryContract Position { get; set; }
    }
}