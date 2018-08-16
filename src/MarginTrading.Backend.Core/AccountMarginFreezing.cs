﻿using System;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public class AccountMarginFreezing : IAccountMarginFreezing
    {
        public AccountMarginFreezing([NotNull] string operationId, [NotNull] string clientId,
            [NotNull] string accountId, decimal amount)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            Amount = amount;
        }

        [NotNull] public string OperationId { get; }
        [NotNull] public string ClientId { get; }
        [NotNull] public string AccountId { get; }
        public decimal Amount { get; }
    }
}