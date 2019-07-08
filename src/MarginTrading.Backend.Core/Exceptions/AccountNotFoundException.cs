// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class AccountNotFoundException : Exception
    {
        public string AccountId { get; private set; }

        public AccountNotFoundException(string accountId, string message):base(message)
        {
            AccountId = accountId;
        }
    }
}