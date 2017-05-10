using System.Collections.Generic;
using System.Linq;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class ClientOrdersBackendResponse
    {
        public OrderBackendContract[] Positions { get; set; }
        public OrderBackendContract[] Orders { get; set; }

        public static ClientOrdersBackendResponse Create(IEnumerable<IOrder> positions, IEnumerable<IOrder> orders)
        {
            return new ClientOrdersBackendResponse
            {
                Positions = positions.Select(item => item.ToBackendContract()).ToArray(),
                Orders = orders.Select(item => item.ToBackendContract()).ToArray()
            };
        }
    }
}
