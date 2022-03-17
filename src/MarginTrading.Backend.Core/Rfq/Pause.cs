// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Common;

namespace MarginTrading.Backend.Core.Rfq
{
    /// <summary>
    /// Special liquidation pause domain object
    /// </summary>
    public class Pause
    {
        public long? Oid { get; }

        public string OperationId { get; }

        public string OperationName { get; }

        public DateTime CreatedAt { get; }

        public DateTime? EffectiveSince { get; }

        public PauseState State { get; }

        public PauseSource Source { get; }

        public Initiator Initiator  { get; }

        public DateTime? CancelledAt { get; }

        public DateTime? CancellationEffectiveSince { get; }

        public Initiator CancellationInitiator { get; }

        public PauseCancellationSource? CancellationSource { get; }

        private Pause(long? oid,
            string operationId,
            string operationName,
            DateTime createdAt,
            DateTime? effectiveSince,
            PauseState state,
            PauseSource source,
            Initiator initiator,
            DateTime? cancelledAt,
            DateTime? cancellationEffectiveSince,
            [CanBeNull] Initiator cancellationInitiator,
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

        public static Pause Initialize(long? oid,
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

        public static Pause Initialize(dynamic o)
        {
            return Initialize(o.Oid,
                o.OperationId,
                o.OperationName,
                o.CreatedAt,
                o.EffectiveSince,
                Enum.Parse<PauseState>(o.State),
                Enum.Parse<PauseSource>(o.Source),
                (Initiator)o.Initiator,
                o.CancelledAt,
                o.CancellationEffectiveSince,
                o.CancellationInitiator == null ? null : new Initiator(o.CancellationInitiator),
                o.CancellationSource == null ? null : Enum.Parse<PauseCancellationSource>(o.CancellationSource));
        }
    }
}