using System;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class ChangeValidityRequest
    {
        public DateTime Validity { get; set; }
        
        public OriginatorTypeContract Originator { get; set; }
        
        public string AdditionalInfo { get; set; }

        public string AccountId { get; set; }
    }
}