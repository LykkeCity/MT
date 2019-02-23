using System;
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
        /// Old value of parameter.
        /// </summary>
        [Key(2)]
        public decimal OldValue { get; set; }
        
        /// <summary>
        /// New value of parameter.
        /// </summary>
        [Key(3)]
        public decimal NewValue { get; set; }
        
        /// <summary>
        /// Indicated if currently applied parameter has been changed.
        /// </summary>
        [Key(4)]
        public bool ChangedActualValue { get; set; }
    }
}