using MarginTrading.Common.Mappers;
using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class OpenOrderBackendResponse
    {
        public OrderBackendContract Order { get; set; }

        public static OpenOrderBackendResponse Create(Order order)
        {
            return new OpenOrderBackendResponse
            {
                Order = order.ToBackendContract()
            };
        }
    }
}
