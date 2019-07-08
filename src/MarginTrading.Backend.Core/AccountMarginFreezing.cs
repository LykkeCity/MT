// Copyright (c) 2019 Lykke Corp.

using System;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public class AccountMarginFreezing : IAccountMarginFreezing
    {
        public AccountMarginFreezing([NotNull] string operationId,
            [NotNull] string accountId, decimal amount)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            Amount = amount;
        }

        [NotNull] public string OperationId { get; }
        [NotNull] public string AccountId { get; }
        public decimal Amount { get; }
    }
}