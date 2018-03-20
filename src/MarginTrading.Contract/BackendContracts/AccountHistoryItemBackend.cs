using System;
using JetBrains.Annotations;

namespace MarginTrading.Contract.BackendContracts
{
    public class AccountHistoryItemBackend
    {
        public DateTime Date { get; set; }
        [CanBeNull] public AccountHistoryBackendContract Account { get; set; }
        [CanBeNull] public OrderHistoryBackendContract Position { get; set; }
    }
}
