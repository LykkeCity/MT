namespace MarginTrading.Backend.Contracts.Trading
{
    /// <summary>
    /// Request for position closing / order cancelling
    /// </summary>
    public class CloseOrderBackendRequest
    {
        public string ClientId { get; set; }
        
        public string OrderId { get; set; }
        
        public string AccountId { get; set; }
        
        /// <summary>
        /// True, if requested by broker
        /// False, if requested by user 
        /// </summary>
        public bool IsForcedByBroker { get; set; }
        
        /// <summary>
        /// Additional info, e.g. broker name or reason of closing
        /// </summary>
        /// <remarks>
        /// Mandatory, if IsForcedByBroker = true
        /// </remarks>
        public string Comment { get; set; } 
    }
}
