// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Contracts.Common;

namespace MarginTrading.Backend.Core.Rfq
{
    /// <summary>
    /// Special liquidation pause domain object
    /// </summary>
    public class Pause
    {
        public readonly int? Oid;

        public readonly string OperationId;

        public readonly string OperationName;

        public readonly DateTime CreatedAt;

        public readonly DateTime? EffectiveSince;

        public readonly PauseState State;

        public readonly PauseSource Source;

        public readonly Initiator Initiator;

        public readonly DateTime? CancelledAt;

        public readonly DateTime? CancellationEffectiveSince;

        public readonly Initiator CancellationInitiator;

        public readonly PauseCancellationSource? CancellationSource;

        private Pause(int? oid,
            string operationId,
            string operationName,
            DateTime createdAt,
            DateTime? effectiveSince,
            PauseState state,
            PauseSource source,
            Initiator initiator,
            DateTime? cancelledAt,
            DateTime? cancellationEffectiveSince,
            Initiator cancellationInitiator,
            PauseCancellationSource? cancellationSource)
        {
            Oid = oid;
            OperationId = operationId;
            OperationName = operationName;
            CreatedAt = createdAt;
            EffectiveSince = effectiveSince;
            State = state;
            Source = source;
            Initiator = initiator;
            CancelledAt = cancelledAt;
            CancellationEffectiveSince = cancellationEffectiveSince;
            CancellationInitiator = cancellationInitiator;
            CancellationSource = cancellationSource;
        }

        public static Pause Create(string operationId,
            string operationName,
            DateTime createdAt,
            DateTime? effectiveSince,
            PauseState state,
            PauseSource source,
            Initiator initiator,
            DateTime? cancelledAt,
            DateTime? cancellationEffectiveSince,
            Initiator cancellationInitiator,
            PauseCancellationSource? cancellationSource) =>
            new Pause(
                null,
                operationId,
                operationName,
                createdAt,
                effectiveSince,
                state,
                source,
                initiator,
                cancelledAt,
                cancellationEffectiveSince,
                cancellationInitiator,
                cancellationSource);

        public static Pause Initialize(int? oid,
            string operationId,
            string operationName,
            DateTime createdAt,
            DateTime? effectiveSince,
            PauseState state,
            PauseSource source,
            Initiator initiator,
            DateTime? cancelledAt,
            DateTime? cancellationEffectiveSince,
            Initiator cancellationInitiator,
            PauseCancellationSource? cancellationSource)
        {
            if (!oid.HasValue)
                throw new InvalidOperationException("Pause initialization can be done for persisted object only");

            return new Pause(
                oid,
                operationId,
                operationName,
                createdAt,
                effectiveSince,
                state,
                source,
                initiator,
                cancelledAt,
                cancellationEffectiveSince,
                cancellationInitiator,
                cancellationSource);
        }
    }
}