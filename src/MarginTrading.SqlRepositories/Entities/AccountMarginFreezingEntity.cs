
using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;

namespace MarginTrading.SqlRepositories.Entities
{
    public class AccountMarginFreezingEntity : IAccountMarginFreezing
    {
        [UsedImplicitly]
        public AccountMarginFreezingEntity([NotNull] string operationId, 
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