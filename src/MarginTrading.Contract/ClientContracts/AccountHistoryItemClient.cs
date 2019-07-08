// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
