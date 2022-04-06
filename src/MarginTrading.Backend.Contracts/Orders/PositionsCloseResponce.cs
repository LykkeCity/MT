// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.Orders
{
    public class PositionCloseResponse
    {
        public string PositionId { get; set; }
        
        public PositionCloseResultContract Result { get; set; }
        
        public string OrderId { get; set; }

        public string ErrorCode { get; set; }
    }
}