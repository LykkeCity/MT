// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.Events
{
    /// <summary>
    /// RFQ event type
    /// </summary>
    public enum RfqEventTypeContract
    {
        /// <summary>
        /// New RFQ appeared
        /// </summary>
        New = 0,
        
        /// <summary>
        /// Existing RFQ has been updated
        /// </summary>
        Update
    }
}