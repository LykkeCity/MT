using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Services.Events
{
    public class LiquidationEndEventArgs
    {
        public string OperationId { get; set; }
        
        public DateTime CreationTime { get; set; }
        
        public string AccountId { get; set; }
        
        public List<string> LiquidatedPositionIds { get; set; }
        
        public string FailReason { get; set; }

        public bool IsSuccess => string.IsNullOrEmpty(FailReason);
    }
}