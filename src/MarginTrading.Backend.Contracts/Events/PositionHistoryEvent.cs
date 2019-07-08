// Copyright (c) 2019 Lykke Corp.

using System;
using MarginTrading.Backend.Contracts.Positions;

namespace MarginTrading.Backend.Contracts.Events
{
    public class PositionHistoryEvent
    {
        /// <summary>
        /// Snapshot of position at the moment of event
        /// </summary>
        public PositionContract PositionSnapshot { get; set; }
        
        /// <summary>
        /// Created deal (if position was closed or partially closed)
        /// </summary>
        public DealContract Deal { get; set; }
        
        /// <summary>
        /// Type of event
        /// </summary>
        public PositionHistoryTypeContract EventType { get; set; }
        
        /// <summary>
        /// Timestamp of event
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Serialised object with additional information for activities in any format
        /// </summary>
        public string ActivitiesMetadata { get; set; }
    }
}