// Copyright (c) 2019 Lykke Corp.

using System;

namespace MarginTrading.Contract.ClientContracts
{
    public class AccountHistoryItemClient
    {
        public DateTime Date { get; set; }
        public AccountHistoryClientContract Account { get; set; }
        public OrderHistoryClientContract Position { get; set; }
    }
}
