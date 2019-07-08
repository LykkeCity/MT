// Copyright (c) 2019 Lykke Corp.

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