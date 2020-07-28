using System;
using System.Collections.Generic;
using System.Text;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class UpdateRelatedOrderBulkRequest
    {
        public IEnumerable<string> PositionIds { get; set; }
        public UpdateRelatedOrderRequest UpdateRelatedOrderRequest { get; set; }
    }
}
