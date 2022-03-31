// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Core.Rfq;

namespace MarginTrading.SqlRepositories.Entities
{
    public class PauseEntity
    {
        public long Oid { get; set; }
        
        public string OperationId { get; set; }
        
        public string OperationName { get; set; }
        
        public PauseSource Source { get; set; }
        
        [CanBeNull] public PauseCancellationSource? CancellationSource { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? EffectiveSince { get; set; }
        
        public PauseState State { get; set; }
        
        public Initiator Initiator { get; set; }
        
        public DateTime? CancelledAt { get; set; }
        
        public DateTime? CancellationEffectiveSince { get; set; }
        
        [CanBeNull] public Initiator CancellationInitiator { get; set; }

        public static PauseEntity Create(Pause pause)
        {
            return new PauseEntity
            {
                Oid = pause.Oid ?? 0,
                OperationId = pause.OperationId,
                OperationName = pause.OperationName,
                Source = pause.Source,
                CancellationSource = pause.CancellationSource,
                CreatedAt = pause.CreatedAt,
                EffectiveSince = pause.EffectiveSince,
                State = pause.State,
                Initiator = pause.Initiator,
                CancelledAt = pause.CancelledAt,
                CancellationEffectiveSince = pause.CancellationEffectiveSince,
                CancellationInitiator = pause.CancellationInitiator
            };
        }
    }
}