// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
