// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands
{
    /// <summary>
    /// Trade request for particular instrument, volume and price.
    /// </summary>
    [MessagePackObject]
    public class ExecuteSpecialLiquidationOrderCommand
    {
        /// <summary>
        /// Operation Id
        /// </summary>
        [Key(0)]
        public string OperationId { get; set; }
        
        /// <summary>
        /// Command creation time
        /// </summary>
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        /// <summary>
        /// Instrument
        /// </summary>
        [Key(3)]
        public string Instrument { get; set; }
        
        /// <summary>
        /// Position volume
        /// </summary>
        [Key(4)]
        public decimal Volume { get; set; }
        
        /// <summary>
        /// Requested execution price
        /// </summary>
        [Key(5)]
        public decimal Price { get; set; }
    }
}