// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Contracts.Rfq
{
    public class RfqPauseInfoContract
    {
        public string State { get; set; }
        
        public string Source { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? EffectiveSince { get; set; }
        
        public string Initiator { get; set; }
    }
}