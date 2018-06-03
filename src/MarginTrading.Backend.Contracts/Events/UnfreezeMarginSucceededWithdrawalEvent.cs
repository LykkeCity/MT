using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Events
{
    /// <summary>
    /// Margin unfreezing has succeeded
    /// </summary>
    [MessagePackObject]
    public class UnfreezeMarginSucceededWithdrawalEvent
    {
        [Key(0)]
        public string OperationId { get; }
        
        [Key(1)]
        public string ClientId { get; }

        [Key(2)]
        public string AccountId { get; }

        [Key(3)]
        public decimal Amount { get; }
        
        public UnfreezeMarginSucceededWithdrawalEvent([NotNull] string operationId, [NotNull] string clientId,
            [NotNull] string accountId, decimal amount)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            Amount = amount;
        }
    }
}