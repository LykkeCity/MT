using System;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class DeleteValidityRequest
    {
        public OriginatorTypeContract Originator { get; set; }
        
        public string AdditionalInfo { get; set; }

        public string AccountId { get; set; }
    }
}