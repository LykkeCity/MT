using System;
using System.Collections.Generic;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Events
{
    /// <summary>
    /// Indicates that persisted value of overnight margin parameter has changed.
    /// </summary>
    [MessagePackObject]
    public class OvernightMarginParameterChangedEvent
    {
        /// <summary>
        /// Id of the process which caused parameter change.
        /// </summary>
        [Key(0)]
        public string CorrelationId { get; set; }
        
        /// <summary>
        /// Time of event generation.
        /// </summary>
        [Key(1)]
        public DateTime EventTimestamp { get; set; }
        
        /// <summary>
        /// Current state of parameter.
        /// </summary>
        [Key(2)]
        public bool CurrentState { get; set; }
        
        /// <summary>
        /// List all items with parameter value != 1. Format: [(TradingCondition, Instrument), Value].
        /// </summary>
        [Key(3)]
        public Dictionary<(string, string), decimal> ParameterValues { get; set; }
    }
}