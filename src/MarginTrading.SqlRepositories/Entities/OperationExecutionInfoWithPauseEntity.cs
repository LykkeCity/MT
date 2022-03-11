// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Core.Rfq;

namespace MarginTrading.SqlRepositories.Entities
{
    public class OperationExecutionInfoWithPauseEntity : OperationExecutionInfoEntity 
    {
        public PauseSource? Source { get; set; }
        
        public PauseCancellationSource? CancellationSource { get; set; }
        
        public DateTime? CreatedAt { get; set; }
        
        public DateTime? EffectiveSince { get; set; }
        
        public PauseState? State { get; set; }
        
        public Initiator? Initiator { get; set; }
        
        public DateTime? CancelledAt { get; set; }
        
        public DateTime? CancellationEffectiveSince { get; set; }
        
        public Initiator? CancellationInitiator { get; set; }
    }
}