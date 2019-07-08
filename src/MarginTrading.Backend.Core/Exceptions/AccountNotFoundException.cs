// Copyright (c) 2019 Lykke Corp.

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