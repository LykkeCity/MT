using System;
using System.Collections.Generic;
using System.Text;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class OrderCancelBulkRequest
    {
        public string[] OrderIds { get; set; }
        public OrderCancelRequest OrderCancelRequest { get; set; }
    }
}
