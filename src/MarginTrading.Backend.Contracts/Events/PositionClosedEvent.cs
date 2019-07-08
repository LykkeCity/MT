// Copyright (c) 2019 Lykke Corp.

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
        /// Position asset pair Id
        /// </summary>
        [NotNull]
        [Key(3)]
        public string AssetPairId { get; }

        /// <summary>
        /// Profit loss which will affect the balance
        /// </summary>
        [Key(4)]
        public decimal BalanceDelta { get; }

        public PositionClosedEvent([NotNull] string accountId, [NotNull] string clientId, [NotNull] string positionId,
            [NotNull] string assetPairId, decimal balanceDelta)
        {
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            PositionId = positionId ?? throw new ArgumentNullException(nameof(positionId));
            AssetPairId = assetPairId ?? throw new ArgumentNullException(nameof(assetPairId));
            BalanceDelta = balanceDelta;
        }
    }
}