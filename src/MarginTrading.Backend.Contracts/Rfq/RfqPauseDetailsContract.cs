// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.Rfq
{
    public class RfqPauseDetailsContract
    {
        public bool IsPaused { get; set; }
        
        public string PauseReason { get; set; }
        
        public string ResumeReason { get; set; }
        
        public bool CanBePaused { get; set; }
        
        public bool CanBeResumed { get; set; }
    }
}