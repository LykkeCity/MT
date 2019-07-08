// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Contract.BackendContracts
{
    public class AccountHistoryBackendRequest
    {
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
