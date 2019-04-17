using System;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class OrderChangeRequest
    {
        public decimal Price { get; set; }
        
        public DateTime? Validity { get; set; }
        
        public OriginatorTypeContract Originator { get; set; }
        
        public string AdditionalInfo { get; set; }
        
        /// <summary>
        /// The correlation identifier. Optional: if not passed will be auto-generated.  
        /// </summary>
        public string CorrelationId { get; set; }
        
        public bool? ForceOpen { get; set; }
    }
}