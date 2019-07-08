// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Contract.ClientContracts
{
    public class AccountHistoryFiltersClientRequest
    {
        public string AccountId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public bool IsLive { get; set; }
    }
}