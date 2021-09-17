using System;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class DeleteValidityRequest
    {
        public OriginatorTypeContract Originator { get; set; }
        
        public string AdditionalInfo { get; set; }
        
        /// <summary>
        /// The correlation identifier. Optional: if not passed will be auto-generated.  
        /// </summary>
        public string CorrelationId { get; set; }

        public string AccountId { get; set; }
    }
}