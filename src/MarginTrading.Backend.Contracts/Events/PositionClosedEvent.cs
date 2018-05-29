using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Events
{
    /// <summary>
    /// Position closed
    /// </summary>
    [MessagePackObject]
    public class PositionClosedEvent
    {
        public PositionClosedEvent([NotNull] string accountId, [NotNull] string clientId, [NotNull] string positionId,
            decimal balanceDelta)
        {
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            PositionId = positionId ?? throw new ArgumentNullException(nameof(positionId));
            BalanceDelta = balanceDelta;
        }

        /// <summary>
        /// Account id
        /// </summary>
        [NotNull]
        [Key(0)]
        public string AccountId { get; }

        /// <summary>
        /// Client id
        /// </summary>
        [NotNull]
        [Key(1)]
        public string ClientId { get; }

        /// <summary>
        /// Closed position id
        /// </summary>
        [NotNull]
        [Key(2)]
        public string PositionId { get; }

        /// <summary>
        /// Profit loss which will affect the balance
        /// </summary>
        [Key(3)]
        public decimal BalanceDelta { get; }
    }
}