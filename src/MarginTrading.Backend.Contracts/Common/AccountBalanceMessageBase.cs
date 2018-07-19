using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.Backend.Contracts
{
    [MessagePackObject]
    public abstract class AccountBalanceMessageBase
    {
        [Key(0)][NotNull]
        public string ClientId { get; }

        [Key(1)][NotNull]
        public string AccountId { get; }

        [Key(2)]
        public decimal Amount { get; }

        [Key(3)][NotNull]
        public string OperationId { get; }

        [Key(4)][NotNull]
        public string Reason { get; }

        protected AccountBalanceMessageBase([NotNull] string clientId, [NotNull] string accountId, decimal amount,
            [NotNull] string operationId, [NotNull] string reason)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            Amount = amount;
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        }
    }
}